using VebTur.Application.Contracts.Reservations;
using FluentValidation;

namespace VebTur.Application.Validation;

public class DeleteReservationRequestsDtoValidator : AbstractValidator<DeleteReservationRequestsDto>
{
    public DeleteReservationRequestsDtoValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().WithMessage("Select at least one reservation to delete.");
    }
}
