namespace VebTur.Application.Contracts.Auth;

public record LoginResponseDto(
    string Token,
    DateTime ExpiresAtUtc,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);
