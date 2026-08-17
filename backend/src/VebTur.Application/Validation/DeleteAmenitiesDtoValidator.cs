using VebTur.Application.Contracts.Admin;
using FluentValidation;

namespace VebTur.Application.Validation;

public class DeleteAmenitiesDtoValidator : AbstractValidator<DeleteAmenitiesDto>
{
    public DeleteAmenitiesDtoValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().WithMessage("Select at least one amenity to delete.");
    }
}
