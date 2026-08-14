using VebTur.Application.Contracts.Notifications;
using FluentValidation;

namespace VebTur.Application.Validation;

public class DeleteNotificationLogsDtoValidator : AbstractValidator<DeleteNotificationLogsDto>
{
    public DeleteNotificationLogsDtoValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().WithMessage("Select at least one notification to delete.");
    }
}
