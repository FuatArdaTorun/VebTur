using VebTur.Application.Common;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Favorites;
using VebTur.Domain.Entities;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Favorites;

public class FavoriteService(VebTurDbContext db) : IFavoriteService
{
    public async Task<PagedResult<HotelSummaryDto>> GetMyFavoritesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query =
            from f in db.Favorites.AsNoTracking()
            join h in db.Hotels.AsNoTracking() on f.HotelId equals h.Id
            where f.UserId == userId && h.IsActive
            orderby f.CreatedAtUtc descending
            select h;

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
                h.GoogleRating,
                h.GoogleRatingCount,
                h.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
                h.RoomTypes.Where(r => r.IsActive).Select(r => (decimal?)r.BaseNightlyPrice).Min(),
                h.RoomTypes.Where(r => r.IsActive).Select(r => r.Currency).FirstOrDefault(),
                db.Reviews.Where(rv => rv.HotelId == h.Id && !rv.IsHidden).Average(rv => (decimal?)rv.Rating),
                db.Reviews.Count(rv => rv.HotelId == h.Id && !rv.IsHidden)))
            .ToListAsync(cancellationToken);

        return new PagedResult<HotelSummaryDto>(items, page, pageSize, totalCount);
    }

    public async Task<List<Guid>> GetMyFavoriteHotelIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.HotelId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddFavoriteAsync(Guid userId, Guid hotelId, CancellationToken cancellationToken)
    {
        var alreadyFavorited = await db.Favorites.AnyAsync(f => f.UserId == userId && f.HotelId == hotelId, cancellationToken);
        if (alreadyFavorited)
        {
            return;
        }

        var hotelExists = await db.Hotels.AnyAsync(h => h.Id == hotelId, cancellationToken);
        if (!hotelExists)
        {
            throw new ValidationException(nameof(hotelId), "Hotel not found.");
        }

        db.Favorites.Add(new Favorite { UserId = userId, HotelId = hotelId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFavoriteAsync(Guid userId, Guid hotelId, CancellationToken cancellationToken)
    {
        var favorite = await db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.HotelId == hotelId, cancellationToken);
        if (favorite is null)
        {
            return;
        }

        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync(cancellationToken);
    }
}
