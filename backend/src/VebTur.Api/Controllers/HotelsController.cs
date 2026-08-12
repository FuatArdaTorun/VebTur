using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Hotels;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

[ApiController]
[Route("api/v1/hotels")]
public class HotelsController(IHotelQueryService hotelQueryService) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<PagedResult<HotelSummaryDto>>> GetHotels(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await hotelQueryService.GetHotelsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{idOrSlug}")]
    public async Task<ActionResult<HotelDetailDto>> GetHotel(string idOrSlug, CancellationToken cancellationToken)
    {
        var hotel = await hotelQueryService.GetHotelByIdOrSlugAsync(idOrSlug, cancellationToken);
        return hotel is null ? NotFound() : Ok(hotel);
    }
}
