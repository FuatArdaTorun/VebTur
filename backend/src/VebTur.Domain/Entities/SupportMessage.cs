using VebTur.Domain.Common;

namespace VebTur.Domain.Entities;

/// <summary>
/// A "Contact Support" message sent from the site's Help page — a lightweight support-ticket
/// record, not tied to any hotel or reservation. An admin's reply is stored/visible in the admin
/// panel only; VebTur has no real email infrastructure, so unlike hotel
/// notifications there is no simulated "sent" step here — this is simply the admin's saved answer.
/// </summary>
public class SupportMessage : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Bare FK, no navigation property — <c>ApplicationUser</c> lives in
    /// <c>VebTur.Infrastructure.Auth</c>, which Domain can never reference (same reasoning as
    /// <see cref="Review"/>.UserId). Nullable — a guest (no account) can contact support too.
    /// </summary>
    public Guid? UserId { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public string? ReplyMessage { get; set; }
    public DateTime? RepliedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
