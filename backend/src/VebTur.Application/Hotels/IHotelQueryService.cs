using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Hotels;

namespace VebTur.Application.Hotels;

public interface IHotelQueryService
{
    Task<PagedResult<HotelSummaryDto>> GetHotelsAsync(HotelSearchRequest request, CancellationToken cancellationToken);

    Task<HotelDetailDto?> GetHotelByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken);

    Task<List<AmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken);
}
