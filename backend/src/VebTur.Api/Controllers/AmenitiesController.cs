using VebTur.Application.Contracts;
using VebTur.Application.Hotels;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

[ApiController]
[Route("api/v1/amenities")]
public class AmenitiesController(IHotelQueryService hotelQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AmenityDto>>> GetAmenities(CancellationToken cancellationToken)
    {
        var amenities = await hotelQueryService.GetAmenitiesAsync(cancellationToken);
        return Ok(amenities);
    }
}
