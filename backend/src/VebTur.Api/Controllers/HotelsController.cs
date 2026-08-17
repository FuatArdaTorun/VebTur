using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.ExternalRatings;
using VebTur.Application.Hotels;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

[ApiController]
[Route("api/v1/hotels")]
public class HotelsController(IHotelQueryService hotelQueryService, IExternalRatingService externalRatingService) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<PagedResult<HotelSummaryDto>>> GetHotels(
        [FromQuery] string? city = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minStarRating = null,
        [FromQuery] string[]? amenities = null,
        [FromQuery] int? minCapacity = null,
        [FromQuery] string sort = "recommended",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var sortOrder = sort.ToLowerInvariant() switch
        {
            "price-asc" => HotelSortOrder.PriceAscending,
            "price-desc" => HotelSortOrder.PriceDescending,
            "star-desc" => HotelSortOrder.StarRatingDescending,
            _ => HotelSortOrder.Recommended,
        };

        var request = new HotelSearchRequest(
            city,
            minPrice,
            maxPrice,
            minStarRating,
            amenities is { Length: > 0 } ? amenities : null,
            minCapacity,
            sortOrder,
            page,
            pageSize);

        var result = await hotelQueryService.GetHotelsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{idOrSlug}")]
    public async Task<ActionResult<HotelDetailDto>> GetHotel(string idOrSlug, CancellationToken cancellationToken)
    {
        var hotel = await hotelQueryService.GetHotelByIdOrSlugAsync(idOrSlug, cancellationToken);
        return hotel is null ? NotFound() : Ok(hotel);
    }

    /// <summary>
    /// Loaded separately from the main hotel detail response so a slow/unreachable external
    /// provider never blocks the rest of the page. 204 when
    /// neither a live nor a demo rating is available for this hotel (not an error case).
    /// </summary>
    [HttpGet("{id:guid}/external-rating")]
    public async Task<ActionResult<ExternalRatingDto>> GetExternalRating(Guid id, CancellationToken cancellationToken)
    {
        var rating = await externalRatingService.GetRatingAsync(id, cancellationToken);
        return rating is null ? NoContent() : Ok(rating);
    }
}
