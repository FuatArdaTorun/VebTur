using VebTur.Application.Contracts.Reservations;
using FluentValidation;

namespace VebTur.Application.Validation;

/// <summary>Same date/count rules as CreateReservationRequestDtoValidator — no guest-detail fields, since those aren't editable (see ReservationRequestService.UpdateMineAsync).</summary>
public class UpdateReservationRequestDtoValidator : AbstractValidator<UpdateReservationRequestDto>
{
    public UpdateReservationRequestDtoValidator()
    {
        RuleFor(x => x.CheckInDate).GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Check-in date cannot be in the past.");
        RuleFor(x => x.CheckOutDate).GreaterThan(x => x.CheckInDate)
            .WithMessage("Check-out date must be after check-in date.");
        RuleFor(x => x.AdultCount).GreaterThan(0);
        RuleFor(x => x.ChildCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SpecialRequests).MaximumLength(1000);
    }
}
