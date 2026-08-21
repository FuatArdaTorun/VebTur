using VebTur.Api.Validation;
using VebTur.Application.Admin;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Reviews;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController(IAdminReviewService adminReviewService, IValidator<DeleteReviewsDto> deleteManyValidator) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminReviewSummaryDto>>> GetReviews(
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
            "hotel-asc" => AdminReviewSortOrder.HotelAscending,
            "hotel-desc" => AdminReviewSortOrder.HotelDescending,
            "reviewer-asc" => AdminReviewSortOrder.ReviewerAscending,
            "reviewer-desc" => AdminReviewSortOrder.ReviewerDescending,
            "rating-asc" => AdminReviewSortOrder.RatingAscending,
            "rating-desc" => AdminReviewSortOrder.RatingDescending,
            "status-asc" => AdminReviewSortOrder.StatusAscending,
            "status-desc" => AdminReviewSortOrder.StatusDescending,
            _ => AdminReviewSortOrder.CreatedDescending,
        };

        var result = await adminReviewService.GetReviewsAsync(new AdminReviewListRequest(search, sortOrder, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/hide")]
    public async Task<IActionResult> HideReview(Guid id, CancellationToken cancellationToken)
    {
        var hidden = await adminReviewService.HideAsync(id, cancellationToken);
        return hidden ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/unhide")]
    public async Task<IActionResult> UnhideReview(Guid id, CancellationToken cancellationToken)
    {
        var unhidden = await adminReviewService.UnhideAsync(id, cancellationToken);
        return unhidden ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await adminReviewService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Bulk permanent delete — irreversible. Ids that no longer exist are silently ignored.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteReviews(DeleteReviewsDto dto, CancellationToken cancellationToken)
    {
        var validation = await deleteManyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        await adminReviewService.DeleteManyAsync(dto.Ids, cancellationToken);
        return NoContent();
    }
}
