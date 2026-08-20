using VebTur.Application.Contracts.Hotels;

namespace VebTur.Application.Hotels;

public interface IRoomTypeAvailabilityService
{
    /// <summary>Null when the room type doesn't exist.</summary>
    Task<RoomTypeAvailabilityDto?> GetBookedDatesAsync(Guid roomTypeId, CancellationToken cancellationToken);
}
