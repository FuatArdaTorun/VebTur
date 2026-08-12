using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Hotels;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Hotels;

public class HotelQueryService(VebTurDbContext db) : IHotelQueryService
{
    public async Task<PagedResult<HotelSummaryDto>> GetHotelsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Hotels.AsNoTracking()
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HotelSummaryDto(
                h.Id,
                h.Name,
                h.Slug,
                h.City,
                h.Country,
                h.StarRating,
                h.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
                h.RoomTypes.Where(r => r.IsActive).Select(r => (decimal?)r.BaseNightlyPrice).Min(),
                h.RoomTypes.Where(r => r.IsActive).Select(r => r.Currency).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<HotelSummaryDto>(items, page, pageSize, totalCount);
    }

    public async Task<HotelDetailDto?> GetHotelByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        var query = db.Hotels.AsNoTracking().Where(h => h.IsActive);

        query = Guid.TryParse(idOrSlug, out var id)
            ? query.Where(h => h.Id == id)
            : query.Where(h => h.Slug == idOrSlug);

        return await query
            .Select(h => new HotelDetailDto(
                h.Id,
                h.Name,
                h.Slug,
                h.Description,
                h.City,
                h.Country,
                h.Address,
                h.Latitude,
                h.Longitude,
                h.StarRating,
                h.OfficialWebsiteUrl,
                h.Images.OrderBy(i => i.DisplayOrder)
                    .Select(i => new HotelImageDto(i.Url, i.AltText, i.DisplayOrder))
                    .ToList(),
                h.HotelAmenities
                    .Select(ha => new AmenityDto(ha.Amenity!.Id, ha.Amenity.Name, ha.Amenity.Slug, ha.Amenity.IconKey))
                    .ToList(),
                h.RoomTypes.Where(r => r.IsActive)
                    .Select(r => new RoomTypeDto(r.Id, r.Name, r.Description, r.Capacity, r.BaseNightlyPrice, r.Currency))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken)
    {
        return await db.Amenities.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AmenityDto(a.Id, a.Name, a.Slug, a.IconKey))
            .ToListAsync(cancellationToken);
    }
}
