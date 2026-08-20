using VebTur.Application.Contracts.Admin;
using FluentValidation;

namespace VebTur.Application.Validation;

public class DeleteSupportMessagesDtoValidator : AbstractValidator<DeleteSupportMessagesDto>
{
    public DeleteSupportMessagesDtoValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().WithMessage("Select at least one message to delete.");
    }
}
