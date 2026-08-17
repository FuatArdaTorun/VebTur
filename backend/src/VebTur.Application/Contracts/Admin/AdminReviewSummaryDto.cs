namespace VebTur.Application.Contracts.Admin;

public record AdminReviewSummaryDto(
    Guid Id,
    Guid HotelId,
    string HotelName,
    string ReviewerName,
    int Rating,
    string? Comment,
    string RoomTypeName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    bool IsHidden,
    DateTime CreatedAtUtc);
