using VebTur.Domain.Common;
using VebTur.Domain.Enums;

namespace VebTur.Domain.Entities;

public class ReservationRequest : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string ReferenceNumber { get; set; }

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public Guid RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }

    /// <summary>
    /// Bare FK, no navigation property — <c>ApplicationUser</c> lives in
    /// <c>VebTur.Infrastructure.Auth</c>, which Domain can never reference. Null for a guest
    /// (no-account) request; set when a logged-in customer submits the request.
    /// </summary>
    public Guid? UserId { get; set; }

    public required string GuestFullName { get; set; }
    public required string GuestEmail { get; set; }
    public required string GuestPhone { get; set; }

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public string? SpecialRequests { get; set; }

    /// <summary>Nights × <see cref="RoomType.BaseNightlyPrice"/> at the time of (re)submission — a snapshot, not a live price.</summary>
    public decimal EstimatedPrice { get; set; }
    public required string Currency { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.AwaitingApproval;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When the demo "sent to the hotel" notification last went out — reset on every re-submission after a customer edit.</summary>
    public DateTime? NotificationSentAtUtc { get; set; }

    public List<NotificationLog> NotificationLogs { get; set; } = [];
}
