using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Hotels;

namespace VebTur.IntegrationTests;

/// <summary>Covers the favorite/bookmark flow: idempotent add/remove, cross-user isolation, and the two read shapes (paginated list, id-only list).</summary>
[Collection(VebTurApiCollection.Name)]
public class FavoritesApiIntegrationTests
{
    private readonly HttpClient _client;

    public FavoritesApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddFavorite_ThenListsInMineAndMineIds()
    {
        var adminToken = await GetAdminTokenAsync();
        var hotelId = await CreateHotelAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();

        Assert.Equal(HttpStatusCode.NoContent, await AddFavoriteAsync(customerToken, hotelId));

        var mine = await GetMineAsync(customerToken);
        Assert.Contains(mine.Items, h => h.Id == hotelId);

        var ids = await GetMineIdsAsync(customerToken);
        Assert.Contains(hotelId, ids);
    }

    [Fact]
    public async Task AddFavorite_Twice_IsIdempotent()
    {
        var adminToken = await GetAdminTokenAsync();
        var hotelId = await CreateHotelAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();

        Assert.Equal(HttpStatusCode.NoContent, await AddFavoriteAsync(customerToken, hotelId));
        Assert.Equal(HttpStatusCode.NoContent, await AddFavoriteAsync(customerToken, hotelId));

        var ids = await GetMineIdsAsync(customerToken);
        Assert.Single(ids, id => id == hotelId);
    }

    [Fact]
    public async Task RemoveFavorite_DropsItFromMineAndMineIds()
    {
        var adminToken = await GetAdminTokenAsync();
        var hotelId = await CreateHotelAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();
        await AddFavoriteAsync(customerToken, hotelId);

        Assert.Equal(HttpStatusCode.NoContent, await RemoveFavoriteAsync(customerToken, hotelId));

        var mine = await GetMineAsync(customerToken);
        Assert.DoesNotContain(mine.Items, h => h.Id == hotelId);

        var ids = await GetMineIdsAsync(customerToken);
        Assert.DoesNotContain(hotelId, ids);
    }

    [Fact]
    public async Task RemoveFavorite_ThatWasNeverAdded_IsIdempotent_ReturnsNoContent()
    {
        var adminToken = await GetAdminTokenAsync();
        var hotelId = await CreateHotelAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();

        Assert.Equal(HttpStatusCode.NoContent, await RemoveFavoriteAsync(customerToken, hotelId));
    }

    [Fact]
    public async Task AddFavorite_ForNonexistentHotel_ReturnsBadRequest()
    {
        var customerToken = await RegisterCustomerAsync();

        Assert.Equal(HttpStatusCode.BadRequest, await AddFavoriteAsync(customerToken, Guid.NewGuid()));
    }

    [Fact]
    public async Task Favorites_AreIsolatedPerUser()
    {
        var adminToken = await GetAdminTokenAsync();
        var hotelId = await CreateHotelAsync(adminToken);
        var owner = await RegisterCustomerAsync();
        var other = await RegisterCustomerAsync();

        await AddFavoriteAsync(owner, hotelId);

        var otherIds = await GetMineIdsAsync(other);
        Assert.DoesNotContain(hotelId, otherIds);
    }

    [Fact]
    public async Task FavoriteEndpoints_WithoutToken_ReturnUnauthorized()
    {
        var hotelId = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/favorites/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/favorites/mine/ids")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsync($"/api/v1/hotels/{hotelId}/favorite", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DeleteAsync($"/api/v1/hotels/{hotelId}/favorite")).StatusCode);
    }

    private async Task<HttpStatusCode> AddFavoriteAsync(string token, Guid hotelId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/hotels/{hotelId}/favorite");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await _client.SendAsync(request)).StatusCode;
    }

    private async Task<HttpStatusCode> RemoveFavoriteAsync(string token, Guid hotelId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/hotels/{hotelId}/favorite");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await _client.SendAsync(request)).StatusCode;
    }

    private async Task<PagedResult<HotelSummaryDto>> GetMineAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/favorites/mine?pageSize=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<HotelSummaryDto>>())!;
    }

    private async Task<List<Guid>> GetMineIdsAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/favorites/mine/ids");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<Guid>>())!;
    }

    private async Task<Guid> CreateHotelAsync(string adminToken)
    {
        var slug = $"favorite-test-{Guid.NewGuid():N}";
        var dto = new AdminHotelUpsertDto(
            Name: "Favorite Test Hotel",
            Slug: slug,
            Description: "Created by a favorites integration test.",
            City: "Antalya",
            Country: "Turkey",
            Address: "Test Address 1",
            Latitude: 36.9,
            Longitude: 30.7,
            StarRating: null,
            OfficialWebsiteUrl: null,
            GooglePlaceId: null,
            PhoneNumber: null,
            GoogleRating: null,
            GoogleRatingCount: null,
            IsActive: true,
            Images: [],
            RoomTypes: [],
            Supervisors: [],
            AmenitySlugs: []);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/hotels") { Content = JsonContent.Create(dto) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<AdminHotelDetailDto>();
        return created!.Id;
    }

    private async Task<string> RegisterCustomerAsync()
    {
        var email = $"favorite-customer-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequestDto(email, "Passw0rd123", "Favorite Test Customer"));
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
