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

    /// <summary>
    /// Irreversibly removes the hotel and its owned children (images, room types, supervisors,
    /// amenity links, external profiles — all <c>Cascade</c> at the DB level)
    /// from the database. Unlike <see cref="DeactivateHotelAsync"/>, there is no undo.
    /// </summary>
    Task<bool> DeleteHotelPermanentlyAsync(Guid id, CancellationToken cancellationToken);
}
