using VebTur.Application.Contracts.Admin;

namespace VebTur.Application.Admin;

public interface IAdminAmenityService
{
    Task<List<AdminAmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken);

    Task<AdminAmenityDto> CreateAmenityAsync(AdminAmenityUpsertDto dto, CancellationToken cancellationToken);

    Task<AdminAmenityDto?> UpdateAmenityAsync(Guid id, AdminAmenityUpsertDto dto, CancellationToken cancellationToken);

    Task<bool> DeleteAmenityAsync(Guid id, CancellationToken cancellationToken);
}
