using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Reservations;

namespace VebTur.Application.Reservations;

public interface IReservationRequestService
{
    Task<ReservationRequestDetailDto> CreateAsync(CreateReservationRequestDto dto, Guid? userId, CancellationToken cancellationToken);

    Task<ReservationRequestDetailDto?> GetByReferenceAsync(string referenceNumber, CancellationToken cancellationToken);

    Task<PagedResult<ReservationRequestDetailDto>> GetMineAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    Task<ReservationRequestDetailDto?> GetMineByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<ReservationRequestDetailDto?> UpdateMineAsync(Guid userId, Guid id, UpdateReservationRequestDto dto, CancellationToken cancellationToken);

    Task<bool> CancelMineAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
