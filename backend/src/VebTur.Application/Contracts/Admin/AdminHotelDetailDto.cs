namespace VebTur.Application.Contracts.Admin;

/// <summary>GET-by-id response — same editable fields as <see cref="AdminHotelUpsertDto"/> plus identity/audit fields.</summary>
public record AdminHotelDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string City,
    string Country,
    string Address,
    double Latitude,
    double Longitude,
    int? StarRating,
    string? OfficialWebsiteUrl,
    string? GooglePlaceId,
    string? PhoneNumber,
    decimal? GoogleRating,
    int? GoogleRatingCount,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<AdminHotelImageDto> Images,
    IReadOnlyList<AdminRoomTypeDto> RoomTypes,
    IReadOnlyList<AdminHotelSupervisorDto> Supervisors,
    IReadOnlyList<string> AmenitySlugs);
