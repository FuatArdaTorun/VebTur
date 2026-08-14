using System.IdentityModel.Tokens.Jwt;
using VebTur.Api.Validation;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Reservations;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

/// <summary>
/// Public by design (no class-level [Authorize]) — guest creation and reference-number lookup
/// must work unauthenticated. The "mine" actions each carry their own [Authorize] instead.
/// </summary>
[ApiController]
[Route("api/v1/reservation-requests")]
public class ReservationRequestsController(
    IReservationRequestService reservationRequestService,
    IValidator<CreateReservationRequestDto> createValidator,
    IValidator<UpdateReservationRequestDto> updateValidator) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpPost]
    public async Task<ActionResult<ReservationRequestDetailDto>> Create(CreateReservationRequestDto dto, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var created = await reservationRequestService.CreateAsync(dto, GetCurrentUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetByReference), new { reference = created.ReferenceNumber }, created);
    }

    [HttpGet("{reference}")]
    public async Task<ActionResult<ReservationRequestDetailDto>> GetByReference(string reference, CancellationToken cancellationToken)
    {
        var reservation = await reservationRequestService.GetByReferenceAsync(reference, cancellationToken);
        return reservation is null ? NotFound() : Ok(reservation);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<PagedResult<ReservationRequestDetailDto>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool sortByUpdated = false,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await reservationRequestService.GetMineAsync(RequireCurrentUserId(), page, pageSize, sortByUpdated, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ReservationRequestDetailDto>> GetMineById(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await reservationRequestService.GetMineByIdAsync(RequireCurrentUserId(), id, cancellationToken);
        return reservation is null ? NotFound() : Ok(reservation);
    }

    [HttpPut("mine/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ReservationRequestDetailDto>> UpdateMine(Guid id, UpdateReservationRequestDto dto, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var updated = await reservationRequestService.UpdateMineAsync(RequireCurrentUserId(), id, dto, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("mine/{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelMine(Guid id, CancellationToken cancellationToken)
    {
        var cancelled = await reservationRequestService.CancelMineAsync(RequireCurrentUserId(), id, cancellationToken);
        return cancelled ? NoContent() : NotFound();
    }

    /// <summary>
    /// Reads the sub claim if a valid bearer token was sent, without requiring one — [Authorize]
    /// doesn't gate this action, but UseAuthentication() still populates User for a valid token.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return sub is not null && Guid.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>Safe to assume present — only called from actions carrying [Authorize].</summary>
    private Guid RequireCurrentUserId() => GetCurrentUserId()!.Value;
}
