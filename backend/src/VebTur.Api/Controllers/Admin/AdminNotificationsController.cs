using VebTur.Api.Validation;
using VebTur.Application.Admin;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Notifications;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/notifications")]
[Authorize(Roles = "Admin")]
public class AdminNotificationsController(
    IAdminNotificationService adminNotificationService,
    IValidator<DeleteNotificationLogsDto> deleteValidator) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminNotificationLogDto>>> GetNotifications(
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
            "sent-asc" => AdminNotificationSortOrder.SentAscending,
            "sent-desc" => AdminNotificationSortOrder.SentDescending,
            "reference-asc" => AdminNotificationSortOrder.ReferenceAscending,
            "reference-desc" => AdminNotificationSortOrder.ReferenceDescending,
            "recipient-asc" => AdminNotificationSortOrder.RecipientAscending,
            "recipient-desc" => AdminNotificationSortOrder.RecipientDescending,
            "subject-asc" => AdminNotificationSortOrder.SubjectAscending,
            "subject-desc" => AdminNotificationSortOrder.SubjectDescending,
            _ => AdminNotificationSortOrder.CreatedDescending,
        };

        var result = await adminNotificationService.GetNotificationsAsync(
            new AdminNotificationListRequest(search, sortOrder, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Bulk delete — irreversible. Ids that no longer exist are silently ignored.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteNotifications(DeleteNotificationLogsDto dto, CancellationToken cancellationToken)
    {
        var validation = await deleteValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        await adminNotificationService.DeleteAsync(dto.Ids, cancellationToken);
        return NoContent();
    }
}
