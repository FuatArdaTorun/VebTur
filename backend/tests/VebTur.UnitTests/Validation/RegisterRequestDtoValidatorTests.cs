using VebTur.Application.Contracts.Auth;
using VebTur.Application.Validation;

namespace VebTur.UnitTests.Validation;

public class RegisterRequestDtoValidatorTests
{
    private readonly RegisterRequestDtoValidator _validator = new();

    private static RegisterRequestDto ValidDto() => new(
        Email: "guest@example.com",
        Password: "Passw0rd",
        DisplayName: "Jane Guest");

    [Fact]
    public void ValidDto_PassesValidation()
    {
        var result = _validator.Validate(ValidDto());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void InvalidEmail_FailsValidation(string email)
    {
        var result = _validator.Validate(ValidDto() with { Email = email });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyPassword_FailsValidation()
    {
        var result = _validator.Validate(ValidDto() with { Password = "" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public void EmptyDisplayName_FailsValidation()
    {
        var result = _validator.Validate(ValidDto() with { DisplayName = "" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DisplayName");
    }
}
