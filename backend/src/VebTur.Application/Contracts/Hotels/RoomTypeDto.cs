namespace VebTur.Application.Contracts.Hotels;

public record RoomTypeDto(
    Guid Id,
    string Name,
    string Description,
    int Capacity,
    decimal BaseNightlyPrice,
    string Currency);
