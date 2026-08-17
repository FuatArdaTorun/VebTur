namespace VebTur.Application.Contracts.Reviews;

public record ReviewDto(
    Guid Id,
    int Rating,
    string? Comment,
    string ReviewerName,
    string RoomTypeName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    DateTime CreatedAtUtc);
