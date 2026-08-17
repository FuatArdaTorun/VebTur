namespace VebTur.Application.Contracts.Reviews;

/// <summary>One of the current customer's own Confirmed reservations at this hotel that doesn't have a review yet.</summary>
public record ReviewableReservationDto(
    Guid ReservationRequestId,
    string RoomTypeName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate);
