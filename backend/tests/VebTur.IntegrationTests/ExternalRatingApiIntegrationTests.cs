using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Hotels;

namespace VebTur.IntegrationTests;

/// <summary>
/// Covers <c>GET /api/v1/hotels/{id}/external-rating</c>: the manual/demo fallback (using each
/// hotel's already-captured <c>GoogleRating</c>/<c>GoogleRatingCount</c>), the "nothing available"
/// case, and caching. The Google Places live path isn't exercised here — the test factory
/// deliberately has no <c>GooglePlaces:ApiKey</c> configured (see VebTurWebApplicationFactory),
/// which is itself the graceful-fallback behavior being verified: a hotel with a GooglePlaceId
/// but no configured key still falls back to Manual rather than erroring.
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class ExternalRatingApiIntegrationTests
{
    private readonly HttpClient _client;

    public ExternalRatingApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExternalRating_WithManuallyCapturedGoogleRating_ReturnsDemoData()
    {
        var token = await GetAdminTokenAsync();
        var hotel = await CreateHotelAsync(token, googlePlaceId: null, googleRating: 4.7m, googleRatingCount: 4158);

        var response = await _client.GetAsync($"/api/v1/hotels/{hotel.Id}/external-rating");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ExternalRatingDto>();
        Assert.NotNull(dto);
        Assert.Equal("Manual", dto!.Provider);
        Assert.True(dto.IsDemoData);
        Assert.Equal(4.7m, dto.Rating);
        Assert.Equal(5m, dto.MaximumRating);
        Assert.Equal(4158, dto.ReviewCount);
    }

    [Fact]
    public async Task ExternalRating_WithGooglePlaceIdButNoApiKeyConfigured_FallsBackToManualDemoData()
    {
        var token = await GetAdminTokenAsync();
        var hotel = await CreateHotelAsync(token, googlePlaceId: "ChIJfake-place-id", googleRating: 4.2m, googleRatingCount: 50);

        var response = await _client.GetAsync($"/api/v1/hotels/{hotel.Id}/external-rating");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ExternalRatingDto>();
        Assert.NotNull(dto);
        Assert.Equal("Manual", dto!.Provider);
        Assert.True(dto.IsDemoData);
    }

    [Fact]
    public async Task ExternalRating_WithNoDataAvailableAnywhere_ReturnsNoContent()
    {
        var token = await GetAdminTokenAsync();
        var hotel = await CreateHotelAsync(token, googlePlaceId: null, googleRating: null, googleRatingCount: null);

        var response = await _client.GetAsync($"/api/v1/hotels/{hotel.Id}/external-rating");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ExternalRating_ForNonexistentHotel_ReturnsNoContent()
    {
        var response = await _client.GetAsync($"/api/v1/hotels/{Guid.NewGuid()}/external-rating");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ExternalRating_IsCachedAcrossCalls_NotRecomputedOnEveryRequest()
    {
        var token = await GetAdminTokenAsync();
        var hotel = await CreateHotelAsync(token, googlePlaceId: null, googleRating: 4.0m, googleRatingCount: 10);

        var first = await _client.GetFromJsonAsync<ExternalRatingDto>($"/api/v1/hotels/{hotel.Id}/external-rating");
        Assert.Equal(4.0m, first!.Rating);

        // Change the underlying manually-captured rating without touching the cache.
        var updateDto = new AdminHotelUpsertDto(
            hotel.Name, hotel.Slug, hotel.Description, hotel.City, hotel.Country, hotel.Address,
            hotel.Latitude, hotel.Longitude, hotel.StarRating, hotel.OfficialWebsiteUrl, hotel.GooglePlaceId, hotel.PhoneNumber,
            GoogleRating: 1.0m, GoogleRatingCount: 999, hotel.IsActive,
            Images: [], RoomTypes: [new AdminRoomTypeDto(null, "Standard Room", "Desc", 2, 1000m, "TRY", 5, true)],
            Supervisors: [], AmenitySlugs: []);
        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/hotels/{hotel.Id}") { Content = JsonContent.Create(updateDto) };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        (await _client.SendAsync(updateRequest)).EnsureSuccessStatusCode();

        // Still the originally-cached value — a fresh manual read would have returned 1.0.
        var second = await _client.GetFromJsonAsync<ExternalRatingDto>($"/api/v1/hotels/{hotel.Id}/external-rating");
        Assert.Equal(4.0m, second!.Rating);
    }

    private async Task<AdminHotelDetailDto> CreateHotelAsync(string adminToken, string? googlePlaceId, decimal? googleRating, int? googleRatingCount)
    {
        var slug = $"external-rating-test-{Guid.NewGuid():N}";
        var dto = new AdminHotelUpsertDto(
            Name: "External Rating Test Hotel",
            Slug: slug,
            Description: "Created by an external rating integration test.",
            City: "Antalya",
            Country: "Turkey",
            Address: "Test Address 1",
            Latitude: 36.9,
            Longitude: 30.7,
            StarRating: null,
            OfficialWebsiteUrl: null,
            GooglePlaceId: googlePlaceId,
            PhoneNumber: null,
            GoogleRating: googleRating,
            GoogleRatingCount: googleRatingCount,
            IsActive: true,
            Images: [],
            RoomTypes: [new AdminRoomTypeDto(null, "Standard Room", "Desc", 2, 1000m, "TRY", 5, true)],
            Supervisors: [],
            AmenitySlugs: []);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/hotels") { Content = JsonContent.Create(dto) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
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
