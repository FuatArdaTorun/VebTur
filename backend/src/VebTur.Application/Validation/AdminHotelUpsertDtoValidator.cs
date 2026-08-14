using VebTur.Application.Contracts.Admin;
using FluentValidation;

namespace VebTur.Application.Validation;

/// <summary>
/// Structural/format checks only (lengths, ranges, patterns). DB-dependent checks — slug
/// uniqueness, whether an amenity slug actually exists — live in AdminHotelService instead,
/// since a validator shouldn't need a DbContext to answer "is this input well-formed."
/// </summary>
public class AdminHotelUpsertDtoValidator : AbstractValidator<AdminHotelUpsertDto>
{
    public AdminHotelUpsertDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase, alphanumeric, and hyphen-separated (e.g. 'my-hotel-name').");
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.StarRating!.Value).InclusiveBetween(1, 5).When(x => x.StarRating.HasValue);
        RuleFor(x => x.OfficialWebsiteUrl).MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("Official website URL must be a valid absolute URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.OfficialWebsiteUrl));
        RuleFor(x => x.GooglePlaceId).MaximumLength(200);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
        RuleFor(x => x.GoogleRating!.Value).InclusiveBetween(0, 5).When(x => x.GoogleRating.HasValue);
        RuleFor(x => x.GoogleRatingCount!.Value).GreaterThanOrEqualTo(0).When(x => x.GoogleRatingCount.HasValue);

        RuleForEach(x => x.Images).ChildRules(image =>
        {
            image.RuleFor(i => i.Url).NotEmpty()
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("Image URL must be a valid absolute URL.");
            image.RuleFor(i => i.AltText).MaximumLength(300);
            image.RuleFor(i => i.DisplayOrder).GreaterThanOrEqualTo(0);
        });

        RuleForEach(x => x.RoomTypes).ChildRules(room =>
        {
            room.RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
            room.RuleFor(r => r.Description).NotEmpty();
            room.RuleFor(r => r.Capacity).GreaterThan(0);
            room.RuleFor(r => r.BaseNightlyPrice).GreaterThanOrEqualTo(0);
            room.RuleFor(r => r.Currency).NotEmpty().Length(3);
            room.RuleFor(r => r.AvailableCount).GreaterThanOrEqualTo(0);
        });

        RuleForEach(x => x.Supervisors).ChildRules(supervisor =>
        {
            supervisor.RuleFor(s => s.FullName).NotEmpty().MaximumLength(200);
            supervisor.RuleFor(s => s.Email).NotEmpty().EmailAddress();
        });

        RuleForEach(x => x.AmenitySlugs).NotEmpty();
    }
}
