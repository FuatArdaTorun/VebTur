namespace VebTur.Application.Contracts.Auth;

public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);
