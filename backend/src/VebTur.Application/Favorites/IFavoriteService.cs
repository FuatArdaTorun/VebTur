using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Hotels;

namespace VebTur.Application.Favorites;

public interface IFavoriteService
{
    Task<PagedResult<HotelSummaryDto>> GetMyFavoritesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    Task<List<Guid>> GetMyFavoriteHotelIdsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Idempotent — favoriting an already-favorited hotel is a no-op success. Throws
    /// <see cref="VebTur.Application.Common.ValidationException"/> (mapped to 400) if the hotel doesn't exist.</summary>
    Task AddFavoriteAsync(Guid userId, Guid hotelId, CancellationToken cancellationToken);

    /// <summary>Idempotent — removing a favorite that isn't there is a no-op success.</summary>
    Task RemoveFavoriteAsync(Guid userId, Guid hotelId, CancellationToken cancellationToken);
}
