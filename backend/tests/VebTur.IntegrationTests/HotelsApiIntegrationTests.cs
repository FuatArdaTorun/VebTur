using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Auth;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Hotels;

namespace VebTur.IntegrationTests;

/// <summary>
/// Covers the public hotel search/filter/sort surface (<see cref="Api.Controllers.HotelsController"/>/
/// <see cref="VebTur.Infrastructure.Hotels.HotelQueryService"/>) and the public amenities/health
/// endpoints — previously only exercised incidentally through <c>AdminApiIntegrationTests</c>'
/// pagination/name-search checks. Each test creates its own uniquely-suffixed test hotels and scopes
/// its query with <c>search=</c> to that suffix, so results are never polluted by the 18 real seeded
/// hotels or by other tests sharing this database.
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class HotelsApiIntegrationTests
{
    private readonly HttpClient _client;

    public HotelsApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHotels_MinPriceFilter_ExcludesCheaperHotels()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var token = await GetAdminTokenAsync();
        var cheap = await CreateHotelAsync(token, suffix, "Cheap", price: 500m);
        var expensive = await CreateHotelAsync(token, suffix, "Expensive", price: 5000m);

        var body = await SearchAsync($"search={suffix}&minPrice=2000");

        Assert.Contains(body.Items, h => h.Id == expensive.Id);
        Assert.DoesNotContain(body.Items, h => h.Id == cheap.Id);
    }

    [Fact]
    public async Task GetHotels_MaxPriceFilter_ExcludesMoreExpensiveHotels()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var token = await GetAdminTokenAsync();
        var cheap = await CreateHotelAsync(token, suffix, "Cheap", price: 500m);
        var expensive = await CreateHotelAsync(token, suffix, "Expensive", price: 5000m);

        var body = await SearchAsync($"search={suffix}&maxPrice=2000");

        Assert.Contains(body.Items, h => h.Id == cheap.Id);
        Assert.DoesNotContain(body.Items, h => h.Id == expensive.Id);
    }

    [Fact]
    public async Task GetHotels_MinStarRatingFilter_ExcludesLowerRatedHotels()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var token = await GetAdminTokenAsync();
        var threeStar = await CreateHotelAsync(token, suffix, "ThreeStar", starRating: 3);
        var fiveStar = await CreateHotelAsync(token, suffix, "FiveStar", starRating: 5);

        var body = await SearchAsync($"search={suffix}&minStarRating=5");

        Assert.Contains(body.Items, h => h.Id == fiveStar.Id);
        Assert.DoesNotContain(body.Items, h => h.Id == threeStar.Id);
    }

    [Fact]
    public async Task GetHotels_MinCapacityFilter_ExcludesHotelsWithNoRoomMeetingCapacity()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var token = await GetAdminTokenAsync();
        var small = await CreateHotelAsync(token, suffix, "Small", capacity: 2);
        var large = await CreateHotelAsync(token, suffix, "Large", capacity: 6);

        var body = await SearchAsync($"search={suffix}&minCapacity=6");

        Assert.Contains(body.Items, h => h.Id == large.Id);
        Assert.DoesNotContain(body.Items, h => h.Id == small.Id);
    }

    [Fact]
    public async Task GetHotels_AmenitySlugFilter_ExcludesHotelsWithoutTheAmenity()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var token = await GetAdminTokenAsync();
        var withPool = await CreateHotelAsync(token, suffix, "WithPool", amenitySlugs: ["pool"]);
        var withoutPool = await CreateHotelAsync(token, suffix, "NoPool", amenitySlugs: []);

        var body = await SearchAsync($"search={suffix}&amenities=pool");

        Assert.Contains(body.Items, h => h.Id == withPool.Id);
        Assert.DoesNotContain(body.Items, h => h.Id == withoutPool.Id);
    }

    [Fact]
    public async Task GetHotels_SortByPriceAscending_OrdersCheapestFirst()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var token = await GetAdminTokenAsync();
        var expensive = await CreateHotelAsync(token, suffix, "Expensive", price: 5000m);
        var cheap = await CreateHotelAsync(token, suffix, "Cheap", price: 500m);

        var body = await SearchAsync($"search={suffix}&sort=price-asc");

        var ids = body.Items.Select(h => h.Id).ToList();
        Assert.True(ids.IndexOf(cheap.Id) < ids.IndexOf(expensive.Id));
    }

    [Fact]
    public async Task GetHotels_SortByPriceDescending_OrdersMostExpensiveFirst()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var token = await GetAdminTokenAsync();
        var expensive = await CreateHotelAsync(token, suffix, "Expensive", price: 5000m);
        var cheap = await CreateHotelAsync(token, suffix, "Cheap", price: 500m);

        var body = await SearchAsync($"search={suffix}&sort=price-desc");

        var ids = body.Items.Select(h => h.Id).ToList();
        Assert.True(ids.IndexOf(expensive.Id) < ids.IndexOf(cheap.Id));
    }

    [Fact]
    public async Task GetHotels_SortByStarRatingDescending_OrdersByGoogleRatingHighestFirst()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var token = await GetAdminTokenAsync();
        var lowRated = await CreateHotelAsync(token, suffix, "LowRated", googleRating: 3.0m);
        var highRated = await CreateHotelAsync(token, suffix, "HighRated", googleRating: 4.9m);

        var body = await SearchAsync($"search={suffix}&sort=star-desc");

        var ids = body.Items.Select(h => h.Id).ToList();
        Assert.True(ids.IndexOf(highRated.Id) < ids.IndexOf(lowRated.Id));
    }

    [Fact]
    public async Task GetHotels_NoMatches_ReturnsEmptyResultNotError()
    {
        var body = await SearchAsync($"search=no-such-hotel-{Guid.NewGuid():N}");

        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
    }

    [Fact]
    public async Task GetHotel_WithUnknownSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/hotels/no-such-hotel-slug-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHotel_WithNonexistentGuid_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/hotels/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAmenities_ReturnsSeededAmenities()
    {
        var response = await _client.GetAsync("/api/v1/amenities");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<List<AmenityDto>>();

        Assert.NotNull(body);
        Assert.Contains(body!, a => a.Slug == "wifi");
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyWithReachableDatabase()
    {
        var response = await _client.GetAsync("/api/v1/health");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<HealthStatusDto>();

        Assert.NotNull(body);
        Assert.Equal("healthy", body!.Status);
        Assert.True(body.DatabaseReachable);
    }

    private async Task<PagedResult<HotelSummaryDto>> SearchAsync(string query)
    {
        var response = await _client.GetAsync($"/api/v1/hotels?{query}&pageSize=50");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<HotelSummaryDto>>())!;
    }

    private async Task<AdminHotelDetailDto> CreateHotelAsync(
        string token,
        string suffix,
        string label,
        decimal price = 1000m,
        int? starRating = null,
        decimal? googleRating = null,
        int capacity = 2,
        IReadOnlyList<string>? amenitySlugs = null)
    {
        var dto = new AdminHotelUpsertDto(
            Name: $"Filter Test {label} {suffix}",
            Slug: $"filter-test-{label.ToLowerInvariant()}-{suffix}",
            Description: "Created by HotelsApiIntegrationTests.",
            City: "Antalya",
            Country: "Turkey",
            Address: "Test Address 1",
            Latitude: 36.9,
            Longitude: 30.7,
            StarRating: starRating,
            OfficialWebsiteUrl: null,
            GooglePlaceId: null,
            PhoneNumber: null,
            GoogleRating: googleRating,
            GoogleRatingCount: googleRating.HasValue ? 10 : null,
            IsActive: true,
            Images: [],
            RoomTypes: [new AdminRoomTypeDto(null, "Standard Room", "Desc", capacity, price, "TRY", 5, true)],
            Supervisors: [],
            AmenitySlugs: amenitySlugs ?? []);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/hotels")
        {
            Content = JsonContent.Create(dto),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AdminHotelDetailDto>())!;
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
