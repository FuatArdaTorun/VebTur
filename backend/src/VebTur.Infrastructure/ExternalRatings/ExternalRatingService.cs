using VebTur.Application.Contracts.Hotels;
using VebTur.Application.ExternalRatings;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.ExternalRatings;

/// <summary>
/// Orchestrates rating lookup: prefer a fresh cached <see cref="ExternalRating"/> row, otherwise
/// try the live Google provider, otherwise fall back to the manual/demo provider — caching
/// whichever succeeds. Deliberately tries Google fresh again (rather than serving a stale Google
/// row) before falling back to manual, since a live call either succeeds with current data or
/// fails fast (no key/place id configured is instant, no network round trip).
/// </summary>
public class ExternalRatingService(
    VebTurDbContext db,
    GooglePlacesRatingProvider googleProvider,
    ManualExternalRatingProvider manualProvider) : IExternalRatingService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<ExternalRatingDto?> GetRatingAsync(Guid hotelId, CancellationToken cancellationToken)
    {
        var hotel = await db.Hotels.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hotelId, cancellationToken);
        if (hotel is null)
        {
            return null;
        }

        var cachedGoogle = await TryGetFreshCacheAsync(hotelId, ExternalProvider.Google, cancellationToken);
        if (cachedGoogle is not null)
        {
            return ToDto(cachedGoogle);
        }

        var googleResult = await googleProvider.TryGetRatingAsync(hotel, cancellationToken);
        if (googleResult is not null)
        {
            var saved = await UpsertAsync(hotelId, ExternalProvider.Google, googleResult, isDemoData: false, cancellationToken);
            return ToDto(saved);
        }

        var cachedManual = await TryGetFreshCacheAsync(hotelId, ExternalProvider.Manual, cancellationToken);
        if (cachedManual is not null)
        {
            return ToDto(cachedManual);
        }

        var manualResult = await manualProvider.TryGetRatingAsync(hotel, cancellationToken);
        if (manualResult is null)
        {
            return null;
        }

        var savedManual = await UpsertAsync(hotelId, ExternalProvider.Manual, manualResult, isDemoData: true, cancellationToken);
        return ToDto(savedManual);
    }

    private async Task<ExternalRating?> TryGetFreshCacheAsync(Guid hotelId, ExternalProvider provider, CancellationToken cancellationToken)
    {
        var cached = await db.ExternalRatings
            .FirstOrDefaultAsync(r => r.HotelId == hotelId && r.Provider == provider, cancellationToken);

        return cached is not null && DateTime.UtcNow - cached.LastUpdatedAtUtc < CacheDuration ? cached : null;
    }

    private async Task<ExternalRating> UpsertAsync(
        Guid hotelId, ExternalProvider provider, ExternalRatingResult result, bool isDemoData, CancellationToken cancellationToken)
    {
        var existing = await db.ExternalRatings
            .FirstOrDefaultAsync(r => r.HotelId == hotelId && r.Provider == provider, cancellationToken);

        if (existing is null)
        {
            existing = new ExternalRating { HotelId = hotelId, Provider = provider };
            db.ExternalRatings.Add(existing);
        }

        existing.Rating = result.Rating;
        existing.MaximumRating = result.MaximumRating;
        existing.ReviewCount = result.ReviewCount;
        existing.IsDemoData = isDemoData;
        existing.LastUpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private static ExternalRatingDto ToDto(ExternalRating rating) => new(
        rating.Provider.ToString(), rating.Rating, rating.MaximumRating, rating.ReviewCount, rating.IsDemoData, rating.LastUpdatedAtUtc);
}
