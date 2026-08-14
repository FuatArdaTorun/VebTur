namespace VebTur.Application.Contracts.Notifications;

public record AdminNotificationLogDto(
    Guid Id,
    Guid ReservationRequestId,
    string ReservationReferenceNumber,
    string HotelName,
    string Type,
    string Recipient,
    string Subject,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc,
    string? ErrorMessage);
