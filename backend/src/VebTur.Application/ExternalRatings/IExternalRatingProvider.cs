using VebTur.Domain.Entities;
using VebTur.Domain.Enums;

namespace VebTur.Application.ExternalRatings;

/// <summary>
/// One source of a hotel rating. Never throws for an ordinary "no data" case (missing
/// configuration, no matching external id, a failed/unreachable call) — returns null instead, so
/// <see cref="IExternalRatingService"/> can fall through to the next provider without a hotel
/// detail page ever breaking because an external dependency misbehaved.
/// </summary>
public interface IExternalRatingProvider
{
    ExternalProvider Provider { get; }

    Task<ExternalRatingResult?> TryGetRatingAsync(Hotel hotel, CancellationToken cancellationToken);
}
