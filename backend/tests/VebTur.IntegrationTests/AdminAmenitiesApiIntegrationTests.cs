using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Auth;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;

namespace VebTur.IntegrationTests;

/// <summary>Covers the bulk-delete endpoint added alongside the existing single create/update/delete CRUD.</summary>
[Collection(VebTurApiCollection.Name)]
public class AdminAmenitiesApiIntegrationTests
{
    private readonly HttpClient _client;

    public AdminAmenitiesApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BulkDelete_RemovesSpecifiedAmenities_AndLeavesOthersUntouched()
    {
        var token = await GetAdminTokenAsync();
        var toDelete = await CreateAmenityAsync(token);
        var toKeep = await CreateAmenityAsync(token);

        Assert.Equal(HttpStatusCode.NoContent, await BulkDeleteAsync(token, [toDelete.Id]));

        var all = await GetAmenitiesAsync(token);
        Assert.DoesNotContain(all, a => a.Id == toDelete.Id);
        Assert.Contains(all, a => a.Id == toKeep.Id);
    }

    [Fact]
    public async Task BulkDelete_WithEmptyIds_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();

        Assert.Equal(HttpStatusCode.BadRequest, await BulkDeleteAsync(token, []));
    }

    [Fact]
    public async Task BulkDelete_WithoutToken_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/amenities")
        {
            Content = JsonContent.Create(new DeleteAmenitiesDto([Guid.NewGuid()])),
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<AdminAmenityDto> CreateAmenityAsync(string token)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/amenities")
        {
            Content = JsonContent.Create(new AdminAmenityUpsertDto($"Bulk Test Amenity {suffix}", $"bulk-test-amenity-{suffix}", null)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AdminAmenityDto>())!;
    }

    private async Task<List<AdminAmenityDto>> GetAmenitiesAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/amenities");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<AdminAmenityDto>>())!;
    }

    private async Task<HttpStatusCode> BulkDeleteAsync(string token, Guid[] ids)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/amenities")
        {
            Content = JsonContent.Create(new DeleteAmenitiesDto(ids)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        return response.StatusCode;
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
