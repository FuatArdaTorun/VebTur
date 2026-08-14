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
            Recipient = BuildRecipient(reservation.Hotel!),
            Subject = $"New reservation request {reservation.ReferenceNumber}",
            Status = NotificationStatus.Sent,
            SentAtUtc = DateTime.UtcNow,
        };

        db.NotificationLogs.Add(log);
        logger.LogInformation("Demo hotel notification logged for reservation {Reference}", reservation.ReferenceNumber);

        return Task.CompletedTask;
    }

    /// <summary>
    /// A snapshot of who this would really have gone to, at the time it was (simulated as) sent —
    /// not recomputed later, so editing a supervisor's email afterward doesn't retroactively
    /// rewrite the audit trail. Requires <c>hotel.Supervisors</c> to already be loaded.
    /// </summary>
    private static string BuildRecipient(Hotel hotel)
    {
        var activeEmails = hotel.Supervisors.Where(s => s.IsActive).Select(s => s.Email).ToList();
        return activeEmails.Count > 0 ? string.Join(", ", activeEmails) : "No active supervisor email on file";
    }
}
