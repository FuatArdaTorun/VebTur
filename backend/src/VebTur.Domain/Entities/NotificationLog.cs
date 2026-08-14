using VebTur.Domain.Enums;

namespace VebTur.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ReservationRequestId { get; set; }
    public ReservationRequest? ReservationRequest { get; set; }

    public NotificationType Type { get; set; }
    public required string Recipient { get; set; }
    public required string Subject { get; set; }
    public NotificationStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
