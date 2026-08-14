using VebTur.Api.Validation;
using VebTur.Application.Admin;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/hotels")]
[Authorize(Roles = "Admin")]
public class AdminHotelsController(
    IAdminHotelService adminHotelService,
    IValidator<AdminHotelUpsertDto> validator) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminHotelSummaryDto>>> GetHotels(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await adminHotelService.GetHotelsAsync(new AdminHotelListRequest(search, isActive, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminHotelDetailDto>> GetHotel(Guid id, CancellationToken cancellationToken)
    {
        var hotel = await adminHotelService.GetHotelByIdAsync(id, cancellationToken);
        return hotel is null ? NotFound() : Ok(hotel);
    }

    [HttpPost]
    public async Task<ActionResult<AdminHotelDetailDto>> CreateHotel(AdminHotelUpsertDto dto, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var created = await adminHotelService.CreateHotelAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetHotel), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminHotelDetailDto>> UpdateHotel(Guid id, AdminHotelUpsertDto dto, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var updated = await adminHotelService.UpdateHotelAsync(id, dto, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateHotel(Guid id, CancellationToken cancellationToken)
    {
        var deactivated = await adminHotelService.DeactivateHotelAsync(id, cancellationToken);
        return deactivated ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateHotel(Guid id, CancellationToken cancellationToken)
    {
        var reactivated = await adminHotelService.ReactivateHotelAsync(id, cancellationToken);
        return reactivated ? NoContent() : NotFound();
    }

    /// <summary>Irreversible — distinct from the soft-delete <see cref="DeactivateHotel"/> above.</summary>
    [HttpDelete("{id:guid}/permanent")]
    public async Task<IActionResult> DeleteHotelPermanently(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await adminHotelService.DeleteHotelPermanentlyAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
