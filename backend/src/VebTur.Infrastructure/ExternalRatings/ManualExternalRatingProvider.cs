using VebTur.Application.ExternalRatings;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;

namespace VebTur.Infrastructure.ExternalRatings;

/// <summary>
/// Demo/manual fallback — used whenever the live Google provider has nothing (no API key
/// configured, no GooglePlaceId on the hotel, or the call failed). Reuses
/// <see cref="Hotel.GoogleRating"/>/<see cref="Hotel.GoogleRatingCount"/>, the real numbers
/// manually captured from each hotel's own Google Maps listing — not fabricated,
/// just not a live API call. Returns null when even that isn't
/// available, rather than inventing a number.
/// </summary>
public class ManualExternalRatingProvider : IExternalRatingProvider
{
    public ExternalProvider Provider => ExternalProvider.Manual;

    public Task<ExternalRatingResult?> TryGetRatingAsync(Hotel hotel, CancellationToken cancellationToken)
    {
        if (hotel.GoogleRating is not { } rating || hotel.GoogleRatingCount is not { } reviewCount)
        {
            return Task.FromResult<ExternalRatingResult?>(null);
        }

        return Task.FromResult<ExternalRatingResult?>(new ExternalRatingResult(rating, MaximumRating: 5m, reviewCount));
    }
}
