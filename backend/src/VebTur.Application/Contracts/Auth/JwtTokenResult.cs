namespace VebTur.Application.Contracts.Auth;

public record JwtTokenResult(string Token, DateTime ExpiresAtUtc);
