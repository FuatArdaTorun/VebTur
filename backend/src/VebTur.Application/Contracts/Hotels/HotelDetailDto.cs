namespace VebTur.Application.Contracts.Hotels;

public record HotelDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string City,
    string Country,
    string Address,
    double Latitude,
    double Longitude,
    int StarRating,
    string? OfficialWebsiteUrl,
    IReadOnlyList<HotelImageDto> Images,
    IReadOnlyList<AmenityDto> Amenities,
    IReadOnlyList<RoomTypeDto> RoomTypes);
