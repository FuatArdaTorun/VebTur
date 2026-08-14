using System.Net;
using System.Net.Http.Json;
using VebTur.Application.Contracts.Auth;

namespace VebTur.IntegrationTests;

/// <summary>
/// Exercises customer self-registration end-to-end against a real Postgres database: the
/// "Customer" role is created by <c>IdentitySeeder.EnsureRolesExistAsync</c> at host startup
/// (not dev-only), <c>POST /api/v1/auth/register</c> creates the account and returns an
/// immediately-usable JWT, and the same account can then log in via the existing endpoint.
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class AuthApiIntegrationTests
{
    private readonly HttpClient _client;

    public AuthApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithNewEmail_CreatesCustomerAccountAndReturnsToken()
    {
        var email = $"guest-{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequestDto(email, "Passw0rd123", "Guest User"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Guest User", body.DisplayName);
        Assert.Contains("Customer", body.Roles);
        Assert.DoesNotContain("Admin", body.Roles);
    }

    [Fact]
    public async Task Register_ThenLogin_WithSameCredentials_Succeeds()
    {
        var email = $"guest-{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd123";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequestDto(email, password, "Guest User"));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, password));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.Contains("Customer", body!.Roles);
    }

    [Fact]
    public async Task Register_WithAlreadyRegisteredEmail_ReturnsBadRequest()
    {
        var email = $"guest-{Guid.NewGuid():N}@example.com";
        var dto = new RegisterRequestDto(email, "Passw0rd123", "Guest User");

        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        var email = $"guest-{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequestDto(email, "weak", "Guest User"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequestDto("not-an-email", "Passw0rd123", "Guest User"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
