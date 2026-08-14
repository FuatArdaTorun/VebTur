namespace VebTur.Application.Contracts.Reservations;

public record AdminReservationSummaryDto(
    Guid Id,
    string ReferenceNumber,
    string HotelName,
    string GuestFullName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    string Status,
    DateTime CreatedAtUtc);
