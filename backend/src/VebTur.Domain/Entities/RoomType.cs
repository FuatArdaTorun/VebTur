namespace VebTur.Domain.Entities;

public class RoomType
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public required string Name { get; set; }
    public required string Description { get; set; }
    public int Capacity { get; set; }

    public decimal BaseNightlyPrice { get; set; }
    public required string Currency { get; set; }

    /// <summary>
    /// A single running counter, not date-scoped inventory (this project has no calendar/
    /// room-instance model). Admin sets the baseline; confirming/cancelling a reservation
    /// against this room type adjusts it automatically — see <c>AdminReservationService</c>.
    /// </summary>
    public int AvailableCount { get; set; }

    public bool IsActive { get; set; } = true;
}
