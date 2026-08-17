namespace VebTur.Application.ExternalRatings;

/// <summary>What a single <see cref="IExternalRatingProvider"/> call produced, before caching.</summary>
public record ExternalRatingResult(decimal Rating, decimal MaximumRating, int ReviewCount);
