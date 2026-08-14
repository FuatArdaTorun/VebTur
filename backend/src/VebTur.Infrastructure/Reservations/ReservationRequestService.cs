using System.Linq.Expressions;
using VebTur.Application.Common;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Reservations;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Reservations;

public class ReservationRequestService(VebTurDbContext db, IHotelNotificationService notificationService) : IReservationRequestService
{
    public async Task<ReservationRequestDetailDto> CreateAsync(CreateReservationRequestDto dto, Guid? userId, CancellationToken cancellationToken)
    {
        var hotel = await db.Hotels.FirstOrDefaultAsync(h => h.Id == dto.HotelId && h.IsActive, cancellationToken)
            ?? throw new ValidationException(nameof(dto.HotelId), "Hotel not found.");

        var roomType = await db.RoomTypes.FirstOrDefaultAsync(r => r.Id == dto.RoomTypeId && r.HotelId == dto.HotelId && r.IsActive, cancellationToken)
            ?? throw new ValidationException(nameof(dto.RoomTypeId), "Room type not found for this hotel.");

        EnsureCapacity(dto.AdultCount, dto.ChildCount, roomType.Capacity);

        var reservation = new ReservationRequest
        {
            ReferenceNumber = await GenerateUniqueReferenceNumberAsync(cancellationToken),
            HotelId = hotel.Id,
            Hotel = hotel,
            RoomTypeId = roomType.Id,
            RoomType = roomType,
            UserId = userId,
            GuestFullName = dto.GuestFullName,
            GuestEmail = dto.GuestEmail,
            GuestPhone = dto.GuestPhone,
            CheckInDate = dto.CheckInDate,
            CheckOutDate = dto.CheckOutDate,
            AdultCount = dto.AdultCount,
            ChildCount = dto.ChildCount,
            SpecialRequests = dto.SpecialRequests,
            EstimatedPrice = ReservationPricingCalculator.CalculateEstimatedPrice(dto.CheckInDate, dto.CheckOutDate, roomType.BaseNightlyPrice),
            Currency = roomType.Currency,
            Status = ReservationStatus.Pending,
        };

        db.ReservationRequests.Add(reservation);

        await SendAndMarkNotifiedAsync(reservation, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDetailDto(reservation, hotel.Name, roomType.Name);
    }

    public async Task<ReservationRequestDetailDto?> GetByReferenceAsync(string referenceNumber, CancellationToken cancellationToken)
        => await db.ReservationRequests.AsNoTracking()
            .Where(r => r.ReferenceNumber == referenceNumber)
            .Select(ProjectToDetailDto)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ReservationRequestDetailDto>> GetMineAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.ReservationRequests.AsNoTracking().Where(r => r.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectToDetailDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReservationRequestDetailDto>(items, page, pageSize, totalCount);
    }

    public async Task<ReservationRequestDetailDto?> GetMineByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => await db.ReservationRequests.AsNoTracking()
            .Where(r => r.Id == id && r.UserId == userId)
            .Select(ProjectToDetailDto)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ReservationRequestDetailDto?> UpdateMineAsync(Guid userId, Guid id, UpdateReservationRequestDto dto, CancellationToken cancellationToken)
    {
        var reservation = await db.ReservationRequests
            .Include(r => r.Hotel)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

        if (reservation is null)
        {
            return null;
        }

        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Sent))
        {
            // Confirmed reservations can no longer be self-edited — silently reopening a settled
            // confirmation (and briefly releasing its held slot) was more surprising than useful;
            // cancelling and submitting a new request is the supported path once confirmed.
            throw new ValidationException(nameof(reservation.Status), "Only pending reservations can be edited — confirmed, cancelled, or rejected reservations can't be changed.");
        }

        var newRoomType = await db.RoomTypes.FirstOrDefaultAsync(r => r.Id == dto.RoomTypeId && r.HotelId == reservation.HotelId && r.IsActive, cancellationToken)
            ?? throw new ValidationException(nameof(dto.RoomTypeId), "Room type not found for this hotel.");

        EnsureCapacity(dto.AdultCount, dto.ChildCount, newRoomType.Capacity);

        reservation.RoomTypeId = newRoomType.Id;
        reservation.RoomType = newRoomType;
        reservation.CheckInDate = dto.CheckInDate;
        reservation.CheckOutDate = dto.CheckOutDate;
        reservation.AdultCount = dto.AdultCount;
        reservation.ChildCount = dto.ChildCount;
        reservation.SpecialRequests = dto.SpecialRequests;
        reservation.EstimatedPrice = ReservationPricingCalculator.CalculateEstimatedPrice(dto.CheckInDate, dto.CheckOutDate, newRoomType.BaseNightlyPrice);
        reservation.Currency = newRoomType.Currency;
        reservation.Status = ReservationStatus.Pending;

        await SendAndMarkNotifiedAsync(reservation, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDetailDto(reservation, reservation.Hotel!.Name, newRoomType.Name);
    }

    public async Task<bool> CancelMineAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var reservation = await db.ReservationRequests
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

        if (reservation is null)
        {
            return false;
        }

        ReservationStatusTransitions.Cancel(reservation);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task SendAndMarkNotifiedAsync(ReservationRequest reservation, CancellationToken cancellationToken)
    {
        await notificationService.NotifyHotelAsync(reservation, cancellationToken);
        reservation.Status = ReservationStatus.Sent;
        reservation.NotificationSentAtUtc = DateTime.UtcNow;
    }

    private static void EnsureCapacity(int adultCount, int childCount, int roomCapacity)
    {
        if (adultCount + childCount > roomCapacity)
        {
            throw new ValidationException(nameof(adultCount), $"This room type accommodates at most {roomCapacity} guests.");
        }
    }

    private async Task<string> GenerateUniqueReferenceNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = ReferenceNumberGenerator.Generate();
            if (!await db.ReservationRequests.AnyAsync(r => r.ReferenceNumber == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique reservation reference number after 5 attempts.");
    }

    private static readonly Expression<Func<ReservationRequest, ReservationRequestDetailDto>> ProjectToDetailDto = r =>
        new ReservationRequestDetailDto(
            r.Id, r.ReferenceNumber, r.HotelId, r.Hotel!.Name, r.RoomTypeId, r.RoomType!.Name,
            r.GuestFullName, r.GuestEmail, r.GuestPhone, r.CheckInDate, r.CheckOutDate,
            r.AdultCount, r.ChildCount, r.SpecialRequests, r.EstimatedPrice, r.Currency,
            r.Status.ToString(), r.CreatedAtUtc, r.UpdatedAtUtc);

    private static ReservationRequestDetailDto ToDetailDto(ReservationRequest r, string hotelName, string roomTypeName) =>
        new(r.Id, r.ReferenceNumber, r.HotelId, hotelName, r.RoomTypeId, roomTypeName,
            r.GuestFullName, r.GuestEmail, r.GuestPhone, r.CheckInDate, r.CheckOutDate,
            r.AdultCount, r.ChildCount, r.SpecialRequests, r.EstimatedPrice, r.Currency,
            r.Status.ToString(), r.CreatedAtUtc, r.UpdatedAtUtc);
}
