namespace VebTur.Application.Contracts.Reservations;

public record ReservationRequestDetailDto(
    Guid Id,
    string ReferenceNumber,
    Guid HotelId,
    string HotelName,
    Guid RoomTypeId,
    string RoomTypeName,
    string GuestFullName,
    string GuestEmail,
    string GuestPhone,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int AdultCount,
    int ChildCount,
    string? SpecialRequests,
    decimal EstimatedPrice,
    string Currency,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
