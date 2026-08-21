using VebTur.Application.Contracts.Auth;
using FluentValidation;

namespace VebTur.Application.Validation;

// Password-policy enforcement stays in Identity's ResetPasswordAsync, same split as ChangePasswordRequestDtoValidator.
public class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty();
    }
}
