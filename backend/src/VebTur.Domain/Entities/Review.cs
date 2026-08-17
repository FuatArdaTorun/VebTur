using VebTur.Domain.Common;

namespace VebTur.Domain.Entities;

public class Review : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public Guid ReservationRequestId { get; set; }
    public ReservationRequest? ReservationRequest { get; set; }

    /// <summary>
    /// Bare FK, no navigation property — <c>ApplicationUser</c> lives in
    /// <c>VebTur.Infrastructure.Auth</c>, which Domain can never reference. Nullable so an
    /// account deletion (SetNull) never destroys the review itself; the reviewer's display name
    /// is read from <see cref="ReservationRequest"/>.GuestFullName instead of stored here.
    /// </summary>
    public Guid? UserId { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public bool IsHidden { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
