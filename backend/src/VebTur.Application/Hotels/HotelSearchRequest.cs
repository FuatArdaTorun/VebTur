namespace VebTur.Application.Hotels;

public record HotelSearchRequest(
    string? City,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinStarRating,
    IReadOnlyList<string>? AmenitySlugs,
    int? MinCapacity,
    HotelSortOrder Sort,
    int Page,
    int PageSize);
