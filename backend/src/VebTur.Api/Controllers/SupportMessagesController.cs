using System.IdentityModel.Tokens.Jwt;
using VebTur.Api.Validation;
using VebTur.Application.Contracts.Support;
using VebTur.Application.Support;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace VebTur.Api.Controllers;

/// <summary>
/// Public by design (no [Authorize]) — anyone, including a guest with no account, can contact
/// support from the site's Help page. Reads the sub claim if a bearer token happens to be
/// present, without requiring one, so a signed-in sender's message is linked to their account —
/// same optional-auth pattern already used by ReviewsController/ReservationRequestsController.
/// </summary>
[ApiController]
[Route("api/v1/support-messages")]
public class SupportMessagesController(ISupportMessageService supportMessageService, IValidator<CreateSupportMessageDto> createValidator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SupportMessageDto>> Create(CreateSupportMessageDto dto, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelStateDictionary());
        }

        var id = await supportMessageService.CreateAsync(GetCurrentUserId(), dto, cancellationToken);
        return Ok(new SupportMessageDto(id, DateTime.UtcNow));
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return sub is not null && Guid.TryParse(sub, out var id) ? id : null;
    }
}
