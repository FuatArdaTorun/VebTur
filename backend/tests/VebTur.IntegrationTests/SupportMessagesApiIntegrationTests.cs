using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Auth;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Support;

namespace VebTur.IntegrationTests;

/// <summary>Covers the "Contact Support" flow: public create, admin list/reply/delete, and role gating.</summary>
[Collection(VebTurApiCollection.Name)]
public class SupportMessagesApiIntegrationTests
{
    private readonly HttpClient _client;

    public SupportMessagesApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsGuest_Succeeds_AndAppearsInAdminList()
    {
        var subject = $"Guest Subject {Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/v1/support-messages",
            new CreateSupportMessageDto("Jane Guest", "jane@example.com", subject, "I have a question about my stay."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SupportMessageDto>();
        Assert.NotNull(body);

        var adminToken = await GetAdminTokenAsync();
        var list = await GetMessagesAsync(adminToken, subject);
        Assert.Contains(list.Items, m => m.Id == body!.Id && m.SenderName == "Jane Guest" && m.ReplyMessage == null);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/support-messages",
            new CreateSupportMessageDto("", "not-an-email", "", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminReply_SetsReplyMessageAndRepliedAtUtc_VisibleOnGetDetail()
    {
        var adminToken = await GetAdminTokenAsync();
        var id = await CreateMessageAsync();

        var replyResponse = await ReplyAsync(adminToken, id, "Thanks for reaching out — here's the answer.");
        Assert.Equal(HttpStatusCode.NoContent, replyResponse.StatusCode);

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/support-messages/{id}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var getResponse = await _client.SendAsync(getRequest);
        var detail = await getResponse.Content.ReadFromJsonAsync<AdminSupportMessageDto>();

        Assert.Equal("Thanks for reaching out — here's the answer.", detail!.ReplyMessage);
        Assert.NotNull(detail.RepliedAtUtc);
    }

    [Fact]
    public async Task AdminReply_WithEmptyText_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();
        var id = await CreateMessageAsync();

        var response = await ReplyAsync(adminToken, id, "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminReply_ForNonexistentMessage_ReturnsNotFound()
    {
        var adminToken = await GetAdminTokenAsync();

        var response = await ReplyAsync(adminToken, Guid.NewGuid(), "Hello");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminDelete_RemovesTheMessage()
    {
        var adminToken = await GetAdminTokenAsync();
        var id = await CreateMessageAsync();

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/support-messages/{id}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/support-messages/{id}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(getRequest)).StatusCode);
    }

    [Fact]
    public async Task AdminBulkDelete_RemovesOnlySelectedMessages()
    {
        var adminToken = await GetAdminTokenAsync();
        var toDelete = await CreateMessageAsync();
        var toKeep = await CreateMessageAsync();

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/support-messages")
        {
            Content = JsonContent.Create(new DeleteSupportMessagesDto([toDelete])),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(request)).StatusCode);

        using var getDeleted = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/support-messages/{toDelete}");
        getDeleted.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(getDeleted)).StatusCode);

        using var getKept = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/support-messages/{toKeep}");
        getKept.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getKept)).StatusCode);
    }

    [Fact]
    public async Task AdminBulkDelete_WithEmptyIds_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/support-messages")
        {
            Content = JsonContent.Create(new DeleteSupportMessagesDto([])),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_WithoutToken_ReturnUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/admin/support-messages")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync($"/api/v1/admin/support-messages/{Guid.NewGuid()}/reply", new ReplySupportMessageDto("hi"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DeleteAsync($"/api/v1/admin/support-messages/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_WithNonAdminRoleToken_ReturnForbidden()
    {
        var customerToken = await RegisterCustomerAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/support-messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task AdminSearch_MatchesSubjectCaseInsensitively()
    {
        var adminToken = await GetAdminTokenAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var subject = $"Refund Question {uniqueSuffix}";
        await PostMessageAsync(subject);

        var list = await GetMessagesAsync(adminToken, subject.ToLowerInvariant()[..10]);
        Assert.Contains(list.Items, m => m.Subject == subject);
    }

    [Fact]
    public async Task AdminMessagesList_SortBySubject_OrdersAscendingAndDescending()
    {
        var adminToken = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var subjectA = $"AAA Sort Test {suffix}";
        var subjectZ = $"ZZZ Sort Test {suffix}";

        (await PostMessageAsync(subjectA)).EnsureSuccessStatusCode();
        (await PostMessageAsync(subjectZ)).EnsureSuccessStatusCode();

        var ascResult = await GetMessagesAsync(adminToken, search: suffix, sort: "subject-asc");
        Assert.Equal([subjectA, subjectZ], ascResult.Items.Select(m => m.Subject));

        var descResult = await GetMessagesAsync(adminToken, search: suffix, sort: "subject-desc");
        Assert.Equal([subjectZ, subjectA], descResult.Items.Select(m => m.Subject));
    }

    private async Task<HttpResponseMessage> PostMessageAsync(string subject, string senderName = "Test Sender") => await _client.PostAsJsonAsync(
        "/api/v1/support-messages",
        new CreateSupportMessageDto(senderName, "sender@example.com", subject, "Message body."));

    private async Task<Guid> CreateMessageAsync()
    {
        var response = await PostMessageAsync($"Test Subject {Guid.NewGuid():N}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SupportMessageDto>();
        return body!.Id;
    }

    private async Task<HttpResponseMessage> ReplyAsync(string token, Guid id, string replyMessage)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/support-messages/{id}/reply")
        {
            Content = JsonContent.Create(new ReplySupportMessageDto(replyMessage)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<PagedResult<AdminSupportMessageDto>> GetMessagesAsync(string token, string? search = null, string? sort = null)
    {
        var query = search is null ? "" : $"?search={Uri.EscapeDataString(search)}&pageSize=50";
        if (sort is not null)
        {
            query += query.Length == 0 ? $"?sort={sort}" : $"&sort={sort}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/support-messages{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<AdminSupportMessageDto>>())!;
    }

    private async Task<string> RegisterCustomerAsync()
    {
        var email = $"support-customer-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequestDto(email, "Passw0rd123", "Support Test Customer"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto(VebTurWebApplicationFactory.AdminEmail, VebTurWebApplicationFactory.AdminPassword));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }
}
