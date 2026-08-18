using VebTur.Application.Contracts.Auth;
using FluentValidation;

namespace VebTur.Application.Validation;

/// <summary>
/// Structural checks only — whether CurrentPassword actually matches and whether NewPassword
/// meets the password policy are both enforced by ASP.NET Core Identity's
/// <c>UserManager.ChangePasswordAsync</c>, same split as elsewhere in this file's siblings.
/// </summary>
public class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty();
    }
}
