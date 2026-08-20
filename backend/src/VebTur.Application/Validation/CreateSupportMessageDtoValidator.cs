using VebTur.Application.Contracts.Support;
using FluentValidation;

namespace VebTur.Application.Validation;

public class CreateSupportMessageDtoValidator : AbstractValidator<CreateSupportMessageDto>
{
    public CreateSupportMessageDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
    }
}
