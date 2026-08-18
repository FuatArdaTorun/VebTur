namespace VebTur.Application.Contracts.Auth;

public record LoginRequestDto(string EmailOrUsername, string Password);
