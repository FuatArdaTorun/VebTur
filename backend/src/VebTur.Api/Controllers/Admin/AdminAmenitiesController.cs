using VebTur.Api.Validation;
using VebTur.Application.Admin;
using VebTur.Application.Contracts.Admin;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/amenities")]
[Authorize(Roles = "Admin")]
public class AdminAmenitiesController(
    IAdminAmenityService adminAmenityService,
    IValidator<AdminAmenityUpsertDto> validator,
    IValidator<DeleteAmenitiesDto> deleteManyValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AdminAmenityDto>>> GetAmenities(CancellationToken cancellationToken)
    {
        return Ok(await adminAmenityService.GetAmenitiesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminAmenityDto>> CreateAmenity(AdminAmenityUpsertDto dto, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var created = await adminAmenityService.CreateAmenityAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetAmenities), created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminAmenityDto>> UpdateAmenity(Guid id, AdminAmenityUpsertDto dto, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var updated = await adminAmenityService.UpdateAmenityAsync(id, dto, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAmenity(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await adminAmenityService.DeleteAmenityAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Bulk delete — irreversible. Ids that no longer exist are silently ignored.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAmenities(DeleteAmenitiesDto dto, CancellationToken cancellationToken)
    {
        var validation = await deleteManyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        await adminAmenityService.DeleteManyAsync(dto.Ids, cancellationToken);
        return NoContent();
    }
}
