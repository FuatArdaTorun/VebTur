using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;

namespace VebTur.Application.Admin;

public interface IAdminHotelService
{
    Task<PagedResult<AdminHotelSummaryDto>> GetHotelsAsync(AdminHotelListRequest request, CancellationToken cancellationToken);

    Task<AdminHotelDetailDto?> GetHotelByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AdminHotelDetailDto> CreateHotelAsync(AdminHotelUpsertDto dto, CancellationToken cancellationToken);

    Task<AdminHotelDetailDto?> UpdateHotelAsync(Guid id, AdminHotelUpsertDto dto, CancellationToken cancellationToken);

    Task<bool> DeactivateHotelAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ReactivateHotelAsync(Guid id, CancellationToken cancellationToken);
}
