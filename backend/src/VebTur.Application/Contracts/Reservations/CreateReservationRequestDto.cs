namespace VebTur.Application.Contracts.Reservations;

public record CreateReservationRequestDto(
    Guid HotelId,
    Guid RoomTypeId,
    string GuestFullName,
    string GuestEmail,
    string GuestPhone,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int AdultCount,
    int ChildCount,
    string? SpecialRequests);
