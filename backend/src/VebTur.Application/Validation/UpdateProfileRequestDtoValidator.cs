using VebTur.Application.Contracts.Auth;
using VebTur.Domain.Enums;
using FluentValidation;

namespace VebTur.Application.Validation;

/// <summary>
/// Structural checks only. Username character-set and uniqueness are enforced by ASP.NET Core
/// Identity's <c>UserManager.SetUserNameAsync</c> (same "let Identity own it" split as the
/// password policy in <see cref="RegisterRequestDtoValidator"/>) — not duplicated here.
/// </summary>
public class UpdateProfileRequestDtoValidator : AbstractValidator<UpdateProfileRequestDto>
{
    private static readonly string[] AllowedGenders = Enum.GetNames<Gender>();

    public UpdateProfileRequestDtoValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(256);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Gender)
            .Must(g => g is null || AllowedGenders.Contains(g, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Gender must be one of: {string.Join(", ", AllowedGenders)}.");
        RuleFor(x => x.DateOfBirth)
            .LessThan(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be in the past.");
    }
}
