namespace VebTur.Application.Contracts.Admin;

/// <summary>
/// Full Hotel aggregate payload for POST (create) and PUT (update) — mirrors the shape
/// HotelSeeder already builds by hand for seed data. Nested collection rows use
/// <c>Id: null</c> for a new row; any existing row whose Id is absent from the submitted
/// list is deleted (see <see cref="VebTur.Application.Admin.HotelAggregateReconciler"/>).
/// </summary>
public record AdminHotelUpsertDto(
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
    IReadOnlyList<AdminHotelImageDto> Images,
    IReadOnlyList<AdminRoomTypeDto> RoomTypes,
    IReadOnlyList<AdminHotelSupervisorDto> Supervisors,
    IReadOnlyList<string> AmenitySlugs);
