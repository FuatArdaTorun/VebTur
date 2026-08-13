using System.IdentityModel.Tokens.Jwt;
using VebTur.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace VebTur.UnitTests.Auth;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int expiryHours = 8) => new(Options.Create(new JwtOptions
    {
        SigningKey = "unit-test-signing-key-at-least-32-bytes-long-for-hmacsha256",
        Issuer = "VebTur.Tests",
        Audience = "VebTur.Tests.Frontend",
        ExpiryHours = expiryHours,
    }));

    [Fact]
    public void GenerateToken_IncludesExpectedClaims()
    {
        var service = CreateService();
        var userId = Guid.CreateVersion7();

        var result = service.GenerateToken(userId, "admin@vebtur.local", ["Admin"]);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(userId.ToString(), token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("admin@vebtur.local", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Admin", token.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("VebTur.Tests", token.Issuer);
        Assert.Contains("VebTur.Tests.Frontend", token.Audiences);
    }

    [Fact]
    public void GenerateToken_SetsExpiryAccordingToConfiguredHours()
    {
        var service = CreateService(expiryHours: 2);
        var before = DateTime.UtcNow;

        var result = service.GenerateToken(Guid.CreateVersion7(), "user@example.com", []);

        var expectedExpiry = before.AddHours(2);
        Assert.True(Math.Abs((result.ExpiresAtUtc - expectedExpiry).TotalSeconds) < 5,
            $"Expected expiry near {expectedExpiry:o}, got {result.ExpiresAtUtc:o}");
    }

    [Fact]
    public void GenerateToken_WithMultipleRoles_IncludesEachAsSeparateRoleClaim()
    {
        var service = CreateService();

        var result = service.GenerateToken(Guid.CreateVersion7(), "user@example.com", ["Admin", "Support"]);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        var roles = token.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();

        Assert.Equal(["Admin", "Support"], roles);
    }

    [Fact]
    public void GenerateToken_WithNoRoles_ProducesNoRoleClaims()
    {
        var service = CreateService();

        var result = service.GenerateToken(Guid.CreateVersion7(), "user@example.com", []);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.DoesNotContain(token.Claims, c => c.Type == "role");
    }
}
