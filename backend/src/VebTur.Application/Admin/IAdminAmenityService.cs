using VebTur.Application.Contracts.Admin;

namespace VebTur.Application.Admin;

public interface IAdminAmenityService
{
    Task<List<AdminAmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken);

    Task<AdminAmenityDto> CreateAmenityAsync(AdminAmenityUpsertDto dto, CancellationToken cancellationToken);

    Task<AdminAmenityDto?> UpdateAmenityAsync(Guid id, AdminAmenityUpsertDto dto, CancellationToken cancellationToken);

    Task<bool> DeleteAmenityAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk delete — no eligibility guard (unlike hotels/reservations), since removing an amenity just unlinks it from any hotels via the Cascade FK on HotelAmenity. Ids that don't exist are silently ignored. Returns how many were actually removed.</summary>
    Task<int> DeleteManyAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
