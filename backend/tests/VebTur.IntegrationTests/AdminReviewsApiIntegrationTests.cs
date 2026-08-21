using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Auth;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Contracts.Reviews;
using Microsoft.Extensions.DependencyInjection;

namespace VebTur.IntegrationTests;

/// <summary>Admin moderation: role gating, search, hide/unhide, and single/bulk permanent delete.</summary>
[Collection(VebTurApiCollection.Name)]
public class AdminReviewsApiIntegrationTests
{
    private readonly VebTurWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminReviewsApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdminReviews_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/admin/reviews");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminReviews_WithNonAdminRoleToken_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/reviews");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetNonAdminToken());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReviews_WithSearch_FindsByHotelName()
    {
        var adminToken = await GetAdminTokenAsync();
        var uniqueHotelName = $"Search Target Hotel {Guid.NewGuid():N}";
        var review = await SeedConfirmedReviewAsync(adminToken, hotelName: uniqueHotelName);

        var found = await SearchAsync(adminToken, uniqueHotelName);

        Assert.Contains(found.Items, r => r.Id == review.Id);
    }

    [Fact]
    public async Task AdminReviewsList_SortByHotel_OrdersAscendingAndDescending()
    {
        var adminToken = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var hotelNameA = $"AAA Sort Test {suffix}";
        var hotelNameZ = $"ZZZ Sort Test {suffix}";

        await SeedConfirmedReviewAsync(adminToken, hotelName: hotelNameA);
        await SeedConfirmedReviewAsync(adminToken, hotelName: hotelNameZ);

        var ascResult = await SearchAsync(adminToken, suffix, sort: "hotel-asc");
        Assert.Equal([hotelNameA, hotelNameZ], ascResult.Items.Select(r => r.HotelName));

        var descResult = await SearchAsync(adminToken, suffix, sort: "hotel-desc");
        Assert.Equal([hotelNameZ, hotelNameA], descResult.Items.Select(r => r.HotelName));
    }

    [Fact]
    public async Task Hide_ExcludesReviewFromPublicListAndCustomerRating_UnhideRestoresIt()
    {
        var adminToken = await GetAdminTokenAsync();
        var review = await SeedConfirmedReviewAsync(adminToken);

        Assert.Equal(HttpStatusCode.NoContent, await HideAsync(adminToken, review.Id));

        var publicList = await _client.GetFromJsonAsync<HotelReviewsResponseDto>($"/api/v1/hotels/{review.HotelId}/reviews");
        Assert.DoesNotContain(publicList!.Reviews, r => r.Id == review.Id);

        var hotelAfterHide = await _client.GetFromJsonAsync<HotelDetailDto>($"/api/v1/hotels/{review.HotelId}");
        Assert.Equal(0, hotelAfterHide!.CustomerReviewCount);

        Assert.Equal(HttpStatusCode.NoContent, await UnhideAsync(adminToken, review.Id));

        var publicListAfterUnhide = await _client.GetFromJsonAsync<HotelReviewsResponseDto>($"/api/v1/hotels/{review.HotelId}/reviews");
        Assert.Contains(publicListAfterUnhide!.Reviews, r => r.Id == review.Id);
    }

    [Fact]
    public async Task Delete_RemovesReviewPermanently()
    {
        var adminToken = await GetAdminTokenAsync();
        var review = await SeedConfirmedReviewAsync(adminToken);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/reviews/{review.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var publicList = await _client.GetFromJsonAsync<HotelReviewsResponseDto>($"/api/v1/hotels/{review.HotelId}/reviews");
        Assert.DoesNotContain(publicList!.Reviews, r => r.Id == review.Id);
    }

    [Fact]
    public async Task BulkDelete_RemovesSelected_AndSilentlyIgnoresUnknownIds()
    {
        var adminToken = await GetAdminTokenAsync();
        var reviewA = await SeedConfirmedReviewAsync(adminToken);
        var reviewB = await SeedConfirmedReviewAsync(adminToken);
        var unknownId = Guid.CreateVersion7();

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/reviews")
        {
            Content = JsonContent.Create(new DeleteReviewsDto([reviewA.Id, reviewB.Id, unknownId])),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var stillThereA = await _client.GetFromJsonAsync<HotelReviewsResponseDto>($"/api/v1/hotels/{reviewA.HotelId}/reviews");
        Assert.DoesNotContain(stillThereA!.Reviews, r => r.Id == reviewA.Id);
    }

    private async Task<PagedResult<AdminReviewSummaryDto>> SearchAsync(string adminToken, string search, string? sort = null)
    {
        var sortQuery = sort is null ? "" : $"&sort={sort}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/reviews?search={Uri.EscapeDataString(search)}&pageSize=200{sortQuery}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<AdminReviewSummaryDto>>())!;
    }

    private async Task<HttpStatusCode> HideAsync(string adminToken, Guid reviewId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reviews/{reviewId}/hide");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        return (await _client.SendAsync(request)).StatusCode;
    }

    private async Task<HttpStatusCode> UnhideAsync(string adminToken, Guid reviewId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reviews/{reviewId}/unhide");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        return (await _client.SendAsync(request)).StatusCode;
    }

    /// <summary>Creates a hotel, a Confirmed reservation for a fresh customer, and a review for it.</summary>
    private async Task<(Guid Id, Guid HotelId)> SeedConfirmedReviewAsync(string adminToken, string? hotelName = null)
    {
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, hotelName);
        var customerToken = await RegisterCustomerAsync();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservation-requests")
        {
            Content = JsonContent.Create(BuildCreateDto(hotelId, roomTypeId)),
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var reservation = (await createResponse.Content.ReadFromJsonAsync<ReservationRequestDetailDto>())!;

        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reservation-requests/{reservation.Id}/confirm");
        confirmRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        (await _client.SendAsync(confirmRequest)).EnsureSuccessStatusCode();

        using var reviewRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/hotels/{hotelId}/reviews")
        {
            Content = JsonContent.Create(new CreateReviewDto(reservation.Id, 4, "Seeded for admin review tests.")),
        };
        reviewRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var reviewResponse = await _client.SendAsync(reviewRequest);
        reviewResponse.EnsureSuccessStatusCode();
        var review = (await reviewResponse.Content.ReadFromJsonAsync<ReviewDto>())!;

        return (review.Id, hotelId);
    }

    private static CreateReservationRequestDto BuildCreateDto(Guid hotelId, Guid roomTypeId) => new(
        hotelId, roomTypeId, "Jane Guest", "jane@example.com", "+90 555 000 00 00",
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
        AdultCount: 2, ChildCount: 0, SpecialRequests: null);

    private async Task<(Guid HotelId, Guid RoomTypeId, string Slug)> CreateHotelWithRoomTypeAsync(string adminToken, string? name)
    {
        var slug = $"admin-review-test-{Guid.NewGuid():N}";
        var dto = new AdminHotelUpsertDto(
            Name: name ?? "Admin Review Test Hotel",
            Slug: slug,
            Description: "Created by an admin review integration test.",
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
            RoomTypes: [new AdminRoomTypeDto(null, "Standard Room", "Desc", 2, 1000m, "TRY", 5, true)],
            Supervisors: [],
            AmenitySlugs: []);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/hotels") { Content = JsonContent.Create(dto) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<AdminHotelDetailDto>();

        return (created!.Id, created.RoomTypes[0].Id!.Value, slug);
    }

    private async Task<string> RegisterCustomerAsync()
    {
        var email = $"admin-review-customer-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequestDto(email, "Passw0rd123", "Admin Review Test Customer"));
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

    /// <summary>A validly-signed token for a user with no roles — proves [Authorize(Roles="Admin")] actually checks the role claim.</summary>
    private string GetNonAdminToken()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtTokenService.GenerateToken(Guid.CreateVersion7(), "nobody@example.com", []).Token;
    }
}
