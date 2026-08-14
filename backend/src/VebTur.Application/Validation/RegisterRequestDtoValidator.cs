using VebTur.Application.Contracts.Auth;
using FluentValidation;

namespace VebTur.Application.Validation;

/// <summary>
/// Structural checks only (format, not-empty). The actual password policy (length, character
/// classes) is enforced by ASP.NET Core Identity's <c>UserManager.CreateAsync</c> — see
/// <c>Program.cs</c>'s <c>AddIdentityCore</c> options — so it isn't duplicated here.
/// </summary>
public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}
