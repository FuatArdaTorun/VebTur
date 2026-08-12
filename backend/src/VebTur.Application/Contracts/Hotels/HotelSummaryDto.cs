namespace VebTur.Application.Contracts.Hotels;

public record HotelSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string City,
    string Country,
    int? StarRating,
    decimal? GoogleRating,
    int? GoogleRatingCount,
    string? ThumbnailUrl,
    decimal? StartingNightlyPrice,
    string? Currency);
