using System.Net;
using System.Net.Http.Headers;
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
    public async Task Register_ThenLogin_WithEmail_Succeeds()
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
    public async Task Register_ThenLogin_WithCustomUsername_Succeeds()
    {
        var email = $"guest-{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd123";
        var username = $"selin_{Guid.NewGuid():N}"[..20];

        var token = await RegisterAsync(email, password);
        await SetUserNameAsync(token, username);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(username, password));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // The old identifier (email) still works too — UserName is an alternative, not a replacement.
        var emailLoginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, password));
        Assert.Equal(HttpStatusCode.OK, emailLoginResponse.StatusCode);
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

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsProfileWithUserNameDefaultingToEmailAndNullPhone()
    {
        var email = $"guest-{Guid.NewGuid():N}@example.com";
        var token = await RegisterAsync(email);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal("Guest User", body!.DisplayName);
        Assert.Equal(email, body.UserName);
        Assert.Null(body.PhoneNumber);
        Assert.Contains("Customer", body.Roles);
    }

    [Fact]
    public async Task UpdateMe_ChangesUsernameAndProfileDetails_LeavesDisplayNameUntouched_AndGetMeReflectsIt()
    {
        var token = await RegisterAsync();
        var dob = new DateOnly(1995, 6, 15);
        var newUsername = $"selin_{Guid.NewGuid():N}"[..20];

        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequestDto(newUsername, "+90 555 111 22 33", "Selin", "Yildiz", "Female", dob)),
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var updateResponse = await _client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal(newUsername, updated!.UserName);
        Assert.Equal("Guest User", updated.DisplayName); // untouched — not part of this endpoint
        Assert.Equal("+90 555 111 22 33", updated.PhoneNumber);
        Assert.Equal("Selin", updated.FirstName);
        Assert.Equal("Yildiz", updated.LastName);
        Assert.Equal("Female", updated.Gender);
        Assert.Equal(dob, updated.DateOfBirth);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await (await _client.SendAsync(meRequest)).Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal(newUsername, me!.UserName);
        Assert.Equal("+90 555 111 22 33", me.PhoneNumber);
        Assert.Equal("Selin", me.FirstName);
        Assert.Equal("Yildiz", me.LastName);
        Assert.Equal("Female", me.Gender);
        Assert.Equal(dob, me.DateOfBirth);
    }

    [Fact]
    public async Task UpdateMe_WithEmptyUserName_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequestDto("", null, null, null, null, null)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithUsernameAlreadyTakenByAnotherAccount_ReturnsBadRequest()
    {
        var takenUsername = $"taken_{Guid.NewGuid():N}"[..20];
        var firstUserToken = await RegisterAsync();
        await SetUserNameAsync(firstUserToken, takenUsername);

        var secondUserToken = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequestDto(takenUsername, null, null, null, null, null)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secondUserToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithInvalidGender_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequestDto($"user_{Guid.NewGuid():N}"[..20], null, null, null, "NotAGender", null)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithFutureDateOfBirth_ReturnsBadRequest()
    {
        var token = await RegisterAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequestDto($"user_{Guid.NewGuid():N}"[..20], null, null, null, null, futureDate)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/auth/me", new UpdateProfileRequestDto("name", null, null, null, null, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_Succeeds_AndNewPasswordLogsIn()
    {
        var email = $"guest-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Passw0rd123";
        const string newPassword = "NewPassw0rd456";
        var token = await RegisterAsync(email, oldPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequestDto(oldPassword, newPassword)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var oldPasswordLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, oldPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, newPassword));
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        var token = await RegisterAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequestDto("WrongPassword1", "NewPassw0rd456")),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequestDto("a", "b"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> RegisterAsync(string? email = null, string password = "Passw0rd123")
    {
        email ??= $"guest-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequestDto(email, password, "Guest User"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }

    private async Task SetUserNameAsync(string token, string username)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequestDto(username, null, null, null, null, null)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
