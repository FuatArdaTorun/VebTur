namespace VebTur.Application.Contracts.Admin;

public record AdminRoomTypeDto(
    Guid? Id,
    string Name,
    string Description,
    int Capacity,
    decimal BaseNightlyPrice,
    string Currency,
    bool IsActive);
