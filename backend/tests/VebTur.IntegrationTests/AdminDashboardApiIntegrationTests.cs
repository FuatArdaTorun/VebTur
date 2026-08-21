using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Contracts.Support;

namespace VebTur.IntegrationTests;

/// <summary>
/// Covers the admin dashboard summary endpoint. Counts are asserted as before/after deltas
/// rather than absolute values, since the underlying tables are shared across the whole test
/// suite (other tests running in the same collection add their own rows concurrently).
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class AdminDashboardApiIntegrationTests
{
    private readonly HttpClient _client;

    public AdminDashboardApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSummary_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/admin/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithNonAdminToken_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterCustomerAsync());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_AwaitingApprovalReservationsCount_IncreasesByOne_AfterCreatingAReservation()
    {
        var adminToken = await GetAdminTokenAsync();
        var before = await GetSummaryAsync(adminToken);

        var (hotelId, roomTypeId) = await CreateHotelWithRoomTypeAsync(adminToken);
        await _client.PostAsJsonAsync("/api/v1/reservation-requests", new CreateReservationRequestDto(
            hotelId, roomTypeId, "Dashboard Test Guest", "dashboard-test@example.com", "+90 555 000 00 00",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(12),
            AdultCount: 2, ChildCount: 0, SpecialRequests: null));

        var after = await GetSummaryAsync(adminToken);

        Assert.Equal(before.AwaitingApprovalReservationsCount + 1, after.AwaitingApprovalReservationsCount);
        Assert.Equal(before.ActiveHotelsCount + 1, after.ActiveHotelsCount);
    }

    [Fact]
    public async Task GetSummary_NewSupportMessagesCount_IncreasesByOne_AfterSubmittingASupportMessage()
    {
        var adminToken = await GetAdminTokenAsync();
        var before = await GetSummaryAsync(adminToken);

        var response = await _client.PostAsJsonAsync("/api/v1/support-messages",
            new CreateSupportMessageDto("Dashboard Test", "dashboard-test@example.com", "Test subject", "Test message"));
        response.EnsureSuccessStatusCode();

        var after = await GetSummaryAsync(adminToken);

        Assert.Equal(before.NewSupportMessagesCount + 1, after.NewSupportMessagesCount);
    }

    private async Task<AdminDashboardSummaryDto> GetSummaryAsync(string adminToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AdminDashboardSummaryDto>())!;
    }

    private async Task<(Guid HotelId, Guid RoomTypeId)> CreateHotelWithRoomTypeAsync(string adminToken)
    {
        var slug = $"dashboard-test-{Guid.NewGuid():N}";
        var dto = new AdminHotelUpsertDto(
            Name: "Dashboard Test Hotel",
            Slug: slug,
            Description: "Created by a dashboard integration test.",
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

        return (created!.Id, created.RoomTypes[0].Id!.Value);
    }

    private async Task<string> RegisterCustomerAsync()
    {
        var email = $"dashboard-test-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequestDto(email, "Passw0rd123", "Dashboard Test Customer"));
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
