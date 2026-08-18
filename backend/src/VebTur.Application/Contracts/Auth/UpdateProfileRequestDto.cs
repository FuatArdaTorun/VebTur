namespace VebTur.Application.Contracts.Auth;

// DisplayName is deliberately not editable here — it's set once at registration. This endpoint
// covers the account's login-facing UserName plus the customer's own profile details.
public record UpdateProfileRequestDto(
    string UserName,
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    string? Gender,
    DateOnly? DateOfBirth);
