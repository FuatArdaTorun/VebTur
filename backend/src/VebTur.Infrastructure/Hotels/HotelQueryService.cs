using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Hotels;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Hotels;

public class HotelQueryService(VebTurDbContext db) : IHotelQueryService
{
    public async Task<PagedResult<HotelSummaryDto>> GetHotelsAsync(HotelSearchRequest request, CancellationToken cancellationToken)
    {
        var query = db.Hotels.AsNoTracking().Where(h => h.IsActive);

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(h => h.City == request.City);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(h => EF.Functions.ILike(h.Name, pattern) || EF.Functions.ILike(h.City, pattern));
        }

        if (request.MinStarRating.HasValue)
        {
            query = query.Where(h => h.StarRating >= request.MinStarRating.Value);
        }

        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            // Filter on each hotel's starting (minimum) price, so the price shown on the
            // summary card is always consistent with why the hotel matched the filter.
            query = query.Where(h => h.RoomTypes.Any(r => r.IsActive)
                && (!request.MinPrice.HasValue
                    || h.RoomTypes.Where(r => r.IsActive).Min(r => r.BaseNightlyPrice) >= request.MinPrice.Value)
                && (!request.MaxPrice.HasValue
                    || h.RoomTypes.Where(r => r.IsActive).Min(r => r.BaseNightlyPrice) <= request.MaxPrice.Value));
        }

        if (request.MinCapacity.HasValue)
        {
            query = query.Where(h => h.RoomTypes.Any(r => r.IsActive && r.Capacity >= request.MinCapacity.Value));
        }

        if (request.AmenitySlugs is { Count: > 0 })
        {
            foreach (var slug in request.AmenitySlugs)
            {
                query = query.Where(h => h.HotelAmenities.Any(ha => ha.Amenity!.Slug == slug));
            }
        }

        query = request.Sort switch
        {
            HotelSortOrder.PriceAscending => query
                .OrderBy(h => h.RoomTypes.Where(r => r.IsActive).Select(r => (decimal?)r.BaseNightlyPrice).Min())
                .ThenBy(h => h.Name),
            HotelSortOrder.PriceDescending => query
                .OrderByDescending(h => h.RoomTypes.Where(r => r.IsActive).Select(r => (decimal?)r.BaseNightlyPrice).Min())
                .ThenBy(h => h.Name),
            // Coerce null (unrated) to -1 so hotels without a confirmed rating always sort
            // after rated ones, regardless of direction. Sorts by Google rating (the guest
            // rating shown as stars in the UI), not the official star classification.
            HotelSortOrder.StarRatingDescending => query.OrderByDescending(h => h.GoogleRating ?? -1m).ThenBy(h => h.Name),
            _ => query.OrderByDescending(h => h.GoogleRating ?? -1m).ThenBy(h => h.Name),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(h => new HotelSummaryDto(
                h.Id,
                h.Name,
                h.Slug,
                h.City,
                h.Country,
                h.StarRating,
                h.GoogleRating,
                h.GoogleRatingCount,
                h.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
                h.RoomTypes.Where(r => r.IsActive).Select(r => (decimal?)r.BaseNightlyPrice).Min(),
                h.RoomTypes.Where(r => r.IsActive).Select(r => r.Currency).FirstOrDefault(),
                // (decimal?) cast makes Average() return null instead of throwing on zero reviews.
                db.Reviews.Where(rv => rv.HotelId == h.Id && !rv.IsHidden).Average(rv => (decimal?)rv.Rating),
                db.Reviews.Count(rv => rv.HotelId == h.Id && !rv.IsHidden)))
            .ToListAsync(cancellationToken);

        return new PagedResult<HotelSummaryDto>(items, request.Page, request.PageSize, totalCount);
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
                h.GoogleRating,
                h.GoogleRatingCount,
                h.OfficialWebsiteUrl,
                h.PhoneNumber,
                h.Images.OrderBy(i => i.DisplayOrder)
                    .Select(i => new HotelImageDto(i.Url, i.AltText, i.DisplayOrder))
                    .ToList(),
                h.HotelAmenities
                    .Select(ha => new AmenityDto(ha.Amenity!.Id, ha.Amenity.Name, ha.Amenity.Slug, ha.Amenity.IconKey))
                    .ToList(),
                h.RoomTypes.Where(r => r.IsActive)
                    .Select(r => new RoomTypeDto(r.Id, r.Name, r.Description, r.Capacity, r.BaseNightlyPrice, r.Currency, r.AvailableCount))
                    .ToList(),
                // (decimal?) cast makes Average() return null instead of throwing on zero reviews.
                db.Reviews.Where(rv => rv.HotelId == h.Id && !rv.IsHidden).Average(rv => (decimal?)rv.Rating),
                db.Reviews.Count(rv => rv.HotelId == h.Id && !rv.IsHidden)))
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
