using VebTur.Application.Admin;
using VebTur.Application.Common;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Reservations;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Admin;

public class AdminReservationService(VebTurDbContext db) : IAdminReservationService
{
    public async Task<PagedResult<AdminReservationSummaryDto>> GetReservationsAsync(AdminReservationListRequest request, CancellationToken cancellationToken)
    {
        var query = db.ReservationRequests.AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            query = query.Where(r => request.Statuses.Contains(r.Status));
        }

        if (request.HotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == request.HotelId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.ReferenceNumber, pattern) ||
                EF.Functions.ILike(r.GuestFullName, pattern) ||
                EF.Functions.ILike(r.GuestEmail, pattern) ||
                EF.Functions.ILike(r.GuestPhone, pattern) ||
                EF.Functions.ILike(r.Hotel!.Name, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Each branch has a secondary key so equal-primary-key rows (e.g. same hotel) land
        // adjacently in a stable, predictable order rather than an arbitrary DB-decided one.
        query = request.Sort switch
        {
            AdminReservationSortOrder.HotelAscending => query.OrderBy(r => r.Hotel!.Name).ThenBy(r => r.CheckInDate),
            AdminReservationSortOrder.HotelDescending => query.OrderByDescending(r => r.Hotel!.Name).ThenBy(r => r.CheckInDate),
            AdminReservationSortOrder.CheckInAscending => query.OrderBy(r => r.CheckInDate).ThenBy(r => r.Hotel!.Name),
            AdminReservationSortOrder.CheckInDescending => query.OrderByDescending(r => r.CheckInDate).ThenBy(r => r.Hotel!.Name),
            AdminReservationSortOrder.StatusAscending => query.OrderBy(r => r.Status).ThenByDescending(r => r.CreatedAtUtc),
            AdminReservationSortOrder.StatusDescending => query.OrderByDescending(r => r.Status).ThenByDescending(r => r.CreatedAtUtc),
            _ => query.OrderByDescending(r => r.CreatedAtUtc),
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminReservationSummaryDto(
                r.Id, r.ReferenceNumber, r.Hotel!.Name, r.GuestFullName,
                r.CheckInDate, r.CheckOutDate, r.Status.ToString(), r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminReservationSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<AdminReservationDetailDto?> GetReservationByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.ReservationRequests.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new AdminReservationDetailDto(
                r.Id, r.ReferenceNumber, r.HotelId, r.Hotel!.Name, r.RoomTypeId, r.RoomType!.Name, r.RoomType.AvailableCount,
                r.GuestFullName, r.GuestEmail, r.GuestPhone, r.CheckInDate, r.CheckOutDate,
                r.AdultCount, r.ChildCount, r.SpecialRequests, r.EstimatedPrice, r.Currency,
                r.Status.ToString(), r.CreatedAtUtc, r.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await db.ReservationRequests
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (reservation is null)
        {
            return false;
        }

        if (reservation.Status != ReservationStatus.AwaitingApproval)
        {
            throw new ValidationException(nameof(reservation.Status), "Only reservations awaiting approval can be confirmed.");
        }

        if (reservation.RoomType!.AvailableCount <= 0)
        {
            throw new ValidationException(nameof(reservation.RoomType.AvailableCount), "No rooms available for this room type.");
        }

        reservation.RoomType.AvailableCount -= 1;
        reservation.Status = ReservationStatus.Confirmed;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RejectAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await db.ReservationRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (reservation is null)
        {
            return false;
        }

        if (reservation.Status != ReservationStatus.AwaitingApproval)
        {
            throw new ValidationException(nameof(reservation.Status), "Only reservations awaiting approval can be rejected.");
        }

        reservation.Status = ReservationStatus.Rejected;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Admin-side cancel only makes sense from Confirmed — that's the only status where it does
    /// anything Reject doesn't (releasing the held AvailableCount slot). For a still-AwaitingApproval
    /// reservation, Cancel and Reject are otherwise identical (terminal status flip, no availability
    /// change), so AwaitingApproval is deliberately excluded here even though the shared
    /// <see cref="ReservationStatusTransitions.Cancel"/> helper itself would allow it — that broader
    /// allowance is for the customer's own self-service cancel (<c>ReservationRequestService</c>),
    /// which has no separate "reject" concept and must let a customer withdraw a still-pending request.
    /// </summary>
    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await db.ReservationRequests
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (reservation is null)
        {
            return false;
        }

        if (reservation.Status == ReservationStatus.AwaitingApproval)
        {
            throw new ValidationException(nameof(reservation.Status), "A reservation still awaiting approval should be rejected, not cancelled.");
        }

        ReservationStatusTransitions.Cancel(reservation);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await db.ReservationRequests
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (reservation is null)
        {
            return false;
        }

        // A still-pending request must be Confirmed/Rejected first — deleting it outright would
        // silently discard a guest's request with no record of it ever having been acted on.
        if (reservation.Status == ReservationStatus.AwaitingApproval)
        {
            throw new ValidationException(nameof(reservation.Status), "A reservation still awaiting approval must be confirmed or rejected before it can be deleted.");
        }

        // Deleting a still-Confirmed reservation must release its held slot, same as cancelling it —
        // otherwise the room stays counted as unavailable forever with no record explaining why.
        if (reservation.Status == ReservationStatus.Confirmed)
        {
            reservation.RoomType!.AvailableCount += 1;
        }

        db.ReservationRequests.Remove(reservation);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DeleteManyAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var reservations = await db.ReservationRequests
            .Include(r => r.RoomType)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var deletable = reservations.Where(r => r.Status != ReservationStatus.AwaitingApproval).ToList();

        foreach (var reservation in deletable.Where(r => r.Status == ReservationStatus.Confirmed))
        {
            reservation.RoomType!.AvailableCount += 1;
        }

        db.ReservationRequests.RemoveRange(deletable);
        await db.SaveChangesAsync(cancellationToken);
        return deletable.Count;
    }
}
