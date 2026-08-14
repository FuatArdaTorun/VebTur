using VebTur.Application.Contracts.Reservations;
using FluentValidation;

namespace VebTur.Application.Validation;

/// <summary>
/// Structural/format checks only. DB-dependent checks (hotel/room type exist and are active,
/// guest count vs. room capacity) live in ReservationRequestService instead — same split as
/// AdminHotelUpsertDtoValidator.
/// </summary>
public class CreateReservationRequestDtoValidator : AbstractValidator<CreateReservationRequestDto>
{
    public CreateReservationRequestDtoValidator()
    {
        RuleFor(x => x.GuestFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GuestEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.GuestPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CheckInDate).GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Check-in date cannot be in the past.");
        RuleFor(x => x.CheckOutDate).GreaterThan(x => x.CheckInDate)
            .WithMessage("Check-out date must be after check-in date.");
        RuleFor(x => x.AdultCount).GreaterThan(0);
        RuleFor(x => x.ChildCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SpecialRequests).MaximumLength(1000);
    }
}
