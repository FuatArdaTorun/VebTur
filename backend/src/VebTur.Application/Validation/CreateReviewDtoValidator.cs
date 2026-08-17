using VebTur.Application.Contracts.Reviews;
using FluentValidation;

namespace VebTur.Application.Validation;

/// <summary>
/// Structural checks only. DB-dependent checks (reservation ownership, Confirmed status, one
/// review per reservation) live in ReviewService instead — same split as
/// CreateReservationRequestDtoValidator.
/// </summary>
public class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(x => x.ReservationRequestId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}
