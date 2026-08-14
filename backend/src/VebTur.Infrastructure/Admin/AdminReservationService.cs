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

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
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

        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Sent))
        {
            throw new ValidationException(nameof(reservation.Status), "Only pending/sent reservations can be confirmed.");
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

        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Sent))
        {
            throw new ValidationException(nameof(reservation.Status), "Only pending/sent reservations can be rejected.");
        }

        reservation.Status = ReservationStatus.Rejected;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await db.ReservationRequests
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (reservation is null)
        {
            return false;
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
}
