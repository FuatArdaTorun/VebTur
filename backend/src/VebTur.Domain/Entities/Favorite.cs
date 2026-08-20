namespace VebTur.Domain.Entities;

/// <summary>
/// A customer bookmarking a hotel for later — purely a convenience list, unlike
/// <see cref="Review"/>/<see cref="ReservationRequest"/> which record real historical facts.
/// No independent content of its own, so both FKs are Cascade (see FavoriteConfiguration):
/// deleting the hotel or the account should just make the bookmark disappear, not block the
/// delete or need a SetNull placeholder.
/// </summary>
public class Favorite
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    /// <summary>
    /// Bare FK, no navigation property — <c>ApplicationUser</c> lives in
    /// <c>VebTur.Infrastructure.Auth</c>, which Domain can never reference (same reasoning as
    /// <see cref="Review"/>.UserId).
    /// </summary>
    public Guid UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
