using VebTur.Application.Reservations;
using VebTur.Domain.Enums;

namespace VebTur.Application.Admin;

public record AdminReservationListRequest(IReadOnlyList<ReservationStatus>? Statuses, Guid? HotelId, string? Search, AdminReservationSortOrder Sort, int Page, int PageSize);
