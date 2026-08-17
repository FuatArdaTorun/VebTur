using VebTur.Application.Contracts.Hotels;

namespace VebTur.Application.ExternalRatings;

public interface IExternalRatingService
{
    /// <summary>
    /// Null when the hotel doesn't exist, or when no provider (live or demo) has anything to
    /// show for it (e.g. no GooglePlaceId and no manually-captured GoogleRating either).
    /// </summary>
    Task<ExternalRatingDto?> GetRatingAsync(Guid hotelId, CancellationToken cancellationToken);
}
