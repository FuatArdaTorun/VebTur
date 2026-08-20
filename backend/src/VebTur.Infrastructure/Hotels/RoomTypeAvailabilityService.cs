using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Hotels;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Hotels;

/// <summary>
/// Reconstructs per-day occupancy from Confirmed reservations, since <c>RoomType.AvailableCount</c>
/// is a single non-date-scoped counter (see RoomType.cs) — there's no calendar/room-instance model
/// to query directly. Total capacity is reconstructed as AvailableCount (what's left right now)
/// plus the count of all currently Confirmed reservations against this room type (each already
/// subtracted from AvailableCount at confirm time) — the same counter AdminReservationService
/// checks before allowing a confirm, so a day this marks "full" is exactly a day where a further
/// confirm against overlapping dates would be blocked by that same rule.
/// </summary>
public class RoomTypeAvailabilityService(VebTurDbContext db) : IRoomTypeAvailabilityService
{
    private const int WindowDays = 365;

    public async Task<RoomTypeAvailabilityDto?> GetBookedDatesAsync(Guid roomTypeId, CancellationToken cancellationToken)
    {
        var roomType = await db.RoomTypes.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomTypeId, cancellationToken);
        if (roomType is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowEnd = today.AddDays(WindowDays);

        var confirmedStays = await db.ReservationRequests.AsNoTracking()
            .Where(r => r.RoomTypeId == roomTypeId && r.Status == ReservationStatus.Confirmed)
            .Select(r => new { r.CheckInDate, r.CheckOutDate })
            .ToListAsync(cancellationToken);

        var totalCapacity = roomType.AvailableCount + confirmedStays.Count;

        var dayCount = windowEnd.DayNumber - today.DayNumber + 1;
        var occupancyDelta = new int[dayCount + 1];

        foreach (var stay in confirmedStays)
        {
            var start = stay.CheckInDate < today ? today : stay.CheckInDate;
            var end = stay.CheckOutDate > windowEnd ? windowEnd : stay.CheckOutDate; // exclusive — the checkout night itself isn't occupied
            var startIndex = start.DayNumber - today.DayNumber;
            var endIndex = end.DayNumber - today.DayNumber;
            if (endIndex > startIndex)
            {
                occupancyDelta[startIndex]++;
                occupancyDelta[endIndex]--;
            }
        }

        var fullyBookedDates = new List<DateOnly>();
        var occupied = 0;
        for (var i = 0; i < dayCount; i++)
        {
            occupied += occupancyDelta[i];
            if (totalCapacity <= 0 || occupied >= totalCapacity)
            {
                fullyBookedDates.Add(today.AddDays(i));
            }
        }

        return new RoomTypeAvailabilityDto(fullyBookedDates);
    }
}
