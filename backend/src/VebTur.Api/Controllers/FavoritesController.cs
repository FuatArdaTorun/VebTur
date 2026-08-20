using System.IdentityModel.Tokens.Jwt;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Favorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

/// <summary>Every action requires a signed-in user (any role) — there is no guest concept for favorites.</summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet("favorites/mine")]
    public async Task<ActionResult<PagedResult<HotelSummaryDto>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await favoriteService.GetMyFavoritesAsync(CurrentUserId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cheap id-only list, loaded separately from any hotel listing so the browsing pages
    /// can mark which cards are already favorited without widening the shared HotelSummaryDto/HotelDetailDto.</summary>
    [HttpGet("favorites/mine/ids")]
    public async Task<ActionResult<List<Guid>>> GetMineIds(CancellationToken cancellationToken)
    {
        var ids = await favoriteService.GetMyFavoriteHotelIdsAsync(CurrentUserId, cancellationToken);
        return Ok(ids);
    }

    [HttpPost("hotels/{hotelId:guid}/favorite")]
    public async Task<IActionResult> AddFavorite(Guid hotelId, CancellationToken cancellationToken)
    {
        await favoriteService.AddFavoriteAsync(CurrentUserId, hotelId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("hotels/{hotelId:guid}/favorite")]
    public async Task<IActionResult> RemoveFavorite(Guid hotelId, CancellationToken cancellationToken)
    {
        await favoriteService.RemoveFavoriteAsync(CurrentUserId, hotelId, cancellationToken);
        return NoContent();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
}
