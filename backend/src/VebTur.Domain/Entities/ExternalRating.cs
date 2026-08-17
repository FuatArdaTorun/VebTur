using VebTur.Domain.Enums;

namespace VebTur.Domain.Entities;

/// <summary>
/// A cached snapshot of a hotel's rating from one external provider.
/// Refetched once stale rather than kept live on every page render.
/// </summary>
public class ExternalRating
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public ExternalProvider Provider { get; set; }
    public decimal Rating { get; set; }
    public decimal MaximumRating { get; set; }
    public int ReviewCount { get; set; }

    /// <summary>True when this came from the manual/demo fallback, not a live provider call.</summary>
    public bool IsDemoData { get; set; }

    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
