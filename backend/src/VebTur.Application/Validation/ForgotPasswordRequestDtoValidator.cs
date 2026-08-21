using VebTur.Application.Contracts.Auth;
using FluentValidation;

namespace VebTur.Application.Validation;

public class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
