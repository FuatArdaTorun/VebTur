namespace VebTur.Application.Contracts.Reservations;

/// <summary>Same shape as <see cref="ReservationRequestDetailDto"/> plus <see cref="RoomTypeAvailableCount"/>, so an admin can see whether confirming is even possible before trying.</summary>
public record AdminReservationDetailDto(
    Guid Id,
    string ReferenceNumber,
    Guid HotelId,
    string HotelName,
    Guid RoomTypeId,
    string RoomTypeName,
    int RoomTypeAvailableCount,
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
