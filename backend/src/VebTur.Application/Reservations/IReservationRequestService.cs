using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Reservations;

namespace VebTur.Application.Reservations;

public interface IReservationRequestService
{
    Task<ReservationRequestDetailDto> CreateAsync(CreateReservationRequestDto dto, Guid? userId, CancellationToken cancellationToken);

    Task<ReservationRequestDetailDto?> GetByReferenceAsync(string referenceNumber, CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="sortByUpdated"/> orders by <c>UpdatedAtUtc</c> descending instead of the
    /// default <c>CreatedAtUtc</c> descending — used by the "recent status changes" notification
    /// bell, which cares about when a reservation last changed state, not when it was first made.
    /// </summary>
    Task<PagedResult<ReservationRequestDetailDto>> GetMineAsync(Guid userId, int page, int pageSize, bool sortByUpdated, CancellationToken cancellationToken);

    Task<ReservationRequestDetailDto?> GetMineByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<ReservationRequestDetailDto?> UpdateMineAsync(Guid userId, Guid id, UpdateReservationRequestDto dto, CancellationToken cancellationToken);

    Task<bool> CancelMineAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
