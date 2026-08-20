using VebTur.Application.Contracts.Admin;
using FluentValidation;

namespace VebTur.Application.Validation;

public class ReplySupportMessageDtoValidator : AbstractValidator<ReplySupportMessageDto>
{
    public ReplySupportMessageDtoValidator()
    {
        RuleFor(x => x.ReplyMessage).NotEmpty().MaximumLength(4000);
    }
}
