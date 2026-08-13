using VebTur.Application.Contracts.Admin;
using FluentValidation;

namespace VebTur.Application.Validation;

public class AdminAmenityUpsertDtoValidator : AbstractValidator<AdminAmenityUpsertDto>
{
    public AdminAmenityUpsertDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase, alphanumeric, and hyphen-separated (e.g. 'sea-view').");
        RuleFor(x => x.IconKey).MaximumLength(100);
    }
}
