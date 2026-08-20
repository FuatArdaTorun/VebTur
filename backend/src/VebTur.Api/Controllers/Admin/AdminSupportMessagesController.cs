using VebTur.Api.Validation;
using VebTur.Application.Admin;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/support-messages")]
[Authorize(Roles = "Admin")]
public class AdminSupportMessagesController(
    IAdminSupportMessageService adminSupportMessageService,
    IValidator<ReplySupportMessageDto> replyValidator,
    IValidator<DeleteSupportMessagesDto> deleteManyValidator) : ControllerBase
{
    private const int MaxPageSize = 50;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminSupportMessageDto>>> GetMessages(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await adminSupportMessageService.GetMessagesAsync(new AdminSupportMessageListRequest(search, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminSupportMessageDto>> GetMessage(Guid id, CancellationToken cancellationToken)
    {
        var message = await adminSupportMessageService.GetMessageAsync(id, cancellationToken);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, ReplySupportMessageDto dto, CancellationToken cancellationToken)
    {
        var validation = await replyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var replied = await adminSupportMessageService.ReplyAsync(id, dto, cancellationToken);
        return replied ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await adminSupportMessageService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Bulk permanent delete — irreversible. Ids that no longer exist are silently ignored.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteMessages(DeleteSupportMessagesDto dto, CancellationToken cancellationToken)
    {
        var validation = await deleteManyValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        await adminSupportMessageService.DeleteManyAsync(dto.Ids, cancellationToken);
        return NoContent();
    }
}
