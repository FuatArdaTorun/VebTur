namespace VebTur.Application.Contracts.Hotels;

public record ExternalRatingDto(
    string Provider,
    decimal Rating,
    decimal MaximumRating,
    int ReviewCount,
    bool IsDemoData,
    DateTime LastUpdatedAtUtc);
