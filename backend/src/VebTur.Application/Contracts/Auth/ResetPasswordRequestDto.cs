namespace VebTur.Application.Contracts.Auth;

public record ResetPasswordRequestDto(string Email, string Token, string NewPassword);
