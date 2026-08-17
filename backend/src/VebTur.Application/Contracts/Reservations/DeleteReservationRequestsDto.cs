namespace VebTur.Application.Contracts.Reservations;

public record DeleteReservationRequestsDto(IReadOnlyList<Guid> Ids);
