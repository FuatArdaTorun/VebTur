namespace VebTur.Application.Contracts.Auth;

public record CurrentUserDto(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
