using VebTur.Api.Validation;
using VebTur.Application.Admin;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Reservations;
using VebTur.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/reservation-requests")]
[Authorize(Roles = "Admin")]
public class AdminReservationsController(
    IAdminReservationService adminReservationService,
    IValidator<DeleteReservationRequestsDto> deleteManyValidator) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminReservationSummaryDto>>> GetReservations(
        [FromQuery] ReservationStatus[]? status = null,
        [FromQuery] Guid? hotelId = null,
        [FromQuery] string? search = null,
        [FromQuery] string sort = "created-desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var sortOrder = sort.ToLowerInvariant() switch
        {
            "hotel-asc" => AdminReservationSortOrder.HotelAscending,
            "hotel-desc" => AdminReservationSortOrder.HotelDescending,
            "checkin-asc" => AdminReservationSortOrder.CheckInAscending,
            "checkin-desc" => AdminReservationSortOrder.CheckInDescending,
            "status-asc" => AdminReservationSortOrder.StatusAscending,
            "status-desc" => AdminReservationSortOrder.StatusDescending,
            _ => AdminReservationSortOrder.CreatedDescending,
        };

        // `status` binds both multiple values (?status=Confirmed&status=Rejected) and a single
        // ?status=X (one-element array) — backward compatible with the single-status dropdown filter.
        var result = await adminReservationService.GetReservationsAsync(
            new AdminReservationListRequest(status, hotelId, search, sortOrder, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminReservationDetailDto>> GetReservation(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await adminReservationService.GetReservationByIdAsync(id, cancellationToken);
        return reservation is null ? NotFound() : Ok(reservation);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var confirmed = await adminReservationService.ConfirmAsync(id, cancellationToken);
        return confirmed ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var rejected = await adminReservationService.RejectAsync(id, cancellationToken);
        return rejected ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var cancelled = await adminReservationService.CancelAsync(id, cancellationToken);
        return cancelled ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await adminReservationService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Bulk delete — irreversible. AwaitingApproval ids are silently skipped (must be confirmed/rejected first); ids that no longer exist are silently ignored.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteReservations(DeleteReservationRequestsDto dto, CancellationToken cancellationToken)
    {
        var validation = await deleteManyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        await adminReservationService.DeleteManyAsync(dto.Ids, cancellationToken);
        return NoContent();
    }
}
