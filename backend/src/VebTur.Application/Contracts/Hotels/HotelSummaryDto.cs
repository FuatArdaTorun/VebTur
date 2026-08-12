namespace VebTur.Application.Contracts.Hotels;

public record HotelSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string City,
    string Country,
    int StarRating,
    string? ThumbnailUrl,
    decimal? StartingNightlyPrice,
    string? Currency);
