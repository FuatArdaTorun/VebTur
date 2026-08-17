using VebTur.Application.Contracts.Admin;
using FluentValidation;

namespace VebTur.Application.Validation;

public class DeleteReviewsDtoValidator : AbstractValidator<DeleteReviewsDto>
{
    public DeleteReviewsDtoValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().WithMessage("Select at least one review to delete.");
    }
}
