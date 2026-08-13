namespace VebTur.Application.Contracts.Admin;

public record AdminHotelImageDto(Guid? Id, string Url, string? AltText, int DisplayOrder);
