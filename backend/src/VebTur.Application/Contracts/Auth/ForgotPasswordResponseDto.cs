namespace VebTur.Application.Contracts.Auth;

// DemoResetLink is null when no account matches the email (keeps the Message generic either way,
// so the response itself doesn't reveal whether the email is registered).
// VebTur returns this link directly instead of emailing it (no real email infrastructure exists).
public record ForgotPasswordResponseDto(string Message, string? DemoResetLink);
