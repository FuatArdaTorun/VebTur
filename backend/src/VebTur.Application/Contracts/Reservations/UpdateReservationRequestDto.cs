namespace VebTur.Application.Contracts.Reservations;

public record UpdateReservationRequestDto(
    Guid RoomTypeId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int AdultCount,
    int ChildCount,
    string? SpecialRequests);
