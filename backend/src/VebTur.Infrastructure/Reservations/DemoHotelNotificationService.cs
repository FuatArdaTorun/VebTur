using VebTur.Application.Reservations;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace VebTur.Infrastructure.Reservations;

/// <summary>
/// Simulated hotel notification — VebTur has no real hotel booking API to call.
/// Adds a NotificationLog row to the tracked DbContext but deliberately does
/// NOT call SaveChangesAsync itself — the caller saves the reservation and this log together in
/// one transaction, same pattern as AdminHotelService adding nested rows via <c>db.Add(...)</c>.
/// </summary>
public class DemoHotelNotificationService(VebTurDbContext db, ILogger<DemoHotelNotificationService> logger) : IHotelNotificationService
{
    public Task NotifyHotelAsync(ReservationRequest reservation, CancellationToken cancellationToken)
    {
        var log = new NotificationLog
        {
            ReservationRequestId = reservation.Id,
            Type = NotificationType.DemoHotelApi,
            Recipient = $"{reservation.Hotel!.Name} booking system (demo)",
            Subject = $"New reservation request {reservation.ReferenceNumber}",
            Status = NotificationStatus.Sent,
            SentAtUtc = DateTime.UtcNow,
        };

        db.NotificationLogs.Add(log);
        logger.LogInformation("Demo hotel notification logged for reservation {Reference}", reservation.ReferenceNumber);

        return Task.CompletedTask;
    }
}
