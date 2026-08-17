using VebTur.Application.Contracts.Admin;
using FluentValidation;

namespace VebTur.Application.Validation;

public class DeleteHotelsDtoValidator : AbstractValidator<DeleteHotelsDto>
{
    public DeleteHotelsDtoValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().WithMessage("Select at least one hotel to delete.");
    }
}
