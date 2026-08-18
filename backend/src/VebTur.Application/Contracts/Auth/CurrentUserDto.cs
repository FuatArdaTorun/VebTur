namespace VebTur.Application.Contracts.Auth;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string UserName,
    string DisplayName,
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    string? Gender,
    DateOnly? DateOfBirth,
    IReadOnlyList<string> Roles);
