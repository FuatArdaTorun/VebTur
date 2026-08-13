namespace VebTur.Application.Contracts.Admin;

public record AdminAmenityDto(Guid Id, string Name, string Slug, string? IconKey, int HotelCount);
