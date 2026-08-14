namespace VebTur.Application.Contracts.Auth;

public record RegisterRequestDto(string Email, string Password, string DisplayName);
