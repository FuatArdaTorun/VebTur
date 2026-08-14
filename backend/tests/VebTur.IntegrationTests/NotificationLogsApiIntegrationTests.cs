using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Auth;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Notifications;
using VebTur.Application.Contracts.Reservations;
using Microsoft.Extensions.DependencyInjection;

namespace VebTur.IntegrationTests;

/// <summary>
/// Covers the admin notification-log inspection endpoint: every reservation creation writes a
/// row via <c>DemoHotelNotificationService</c>, and admins should be able to list
/// and search that audit trail.
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class NotificationLogsApiIntegrationTests
{
    private readonly VebTurWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationLogsApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatingAReservation_WritesANotificationLog_VisibleInAdminList()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 3);
        var reservation = await CreateAsGuestAsync(hotelId, roomTypeId);

        var paged = await ListAsync(adminToken, search: reservation.ReferenceNumber);

        Assert.Single(paged.Items);
        var log = paged.Items[0];
        Assert.Equal(reservation.Id, log.ReservationRequestId);
        Assert.Equal(reservation.ReferenceNumber, log.ReservationReferenceNumber);
    }

    [Fact]
    public async Task Recipient_IsTheHotelsActiveSupervisorEmail()
    {
        var adminToken = await GetAdminTokenAsync();
        var supervisorEmail = $"supervisor-{Guid.NewGuid():N}@example.com";
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 3, supervisorEmail: supervisorEmail);
        var reservation = await CreateAsGuestAsync(hotelId, roomTypeId);

        var paged = await ListAsync(adminToken, search: reservation.ReferenceNumber);

        Assert.Single(paged.Items);
        Assert.Equal(supervisorEmail, paged.Items[0].Recipient);
    }

    [Fact]
    public async Task Recipient_FallsBackToAPlaceholder_WhenTheHotelHasNoActiveSupervisor()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 3);
        var reservation = await CreateAsGuestAsync(hotelId, roomTypeId);

        var paged = await ListAsync(adminToken, search: reservation.ReferenceNumber);

        Assert.Single(paged.Items);
        Assert.Equal("No active supervisor email on file", paged.Items[0].Recipient);
    }

    [Fact]
    public async Task SearchByRecipientEmail_ReturnsOnlyMatchingLogs()
    {
        var adminToken = await GetAdminTokenAsync();
        var supervisorEmail = $"search-recipient-{Guid.NewGuid():N}@example.com";
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, 5, supervisorEmail: supervisorEmail);

        var target = await CreateAsGuestAsync(hotelId, roomTypeId);
        await CreateHotelWithRoomTypeAsync(adminToken, 5); // unrelated hotel/reservation noise

        var paged = await ListAsync(adminToken, search: supervisorEmail);

        Assert.Single(paged.Items);
        Assert.Equal(target.Id, paged.Items[0].ReservationRequestId);
    }

    [Fact]
    public async Task SearchByHotelName_ReturnsOnlyMatchingLogs()
    {
        var adminToken = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, 5, $"Notification Search Hotel {suffix}");

        var target = await CreateAsGuestAsync(hotelId, roomTypeId);
        await CreateHotelWithRoomTypeAsync(adminToken, 5); // unrelated hotel/reservation noise

        var paged = await ListAsync(adminToken, search: suffix);

        Assert.Single(paged.Items);
        Assert.Equal(target.Id, paged.Items[0].ReservationRequestId);
    }

    [Fact]
    public async Task AdminNotifications_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/admin/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminNotifications_WithNonAdminRoleToken_ReturnsForbidden()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var nonAdminToken = jwtTokenService.GenerateToken(Guid.CreateVersion7(), "nobody@example.com", []).Token;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/notifications");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", nonAdminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNotifications_RemovesSpecifiedRows_AndLeavesOthersUntouched()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 5);

        var toDelete = await CreateAsGuestAsync(hotelId, roomTypeId);
        var toKeep = await CreateAsGuestAsync(hotelId, roomTypeId);
        var logToDelete = (await ListAsync(adminToken, search: toDelete.ReferenceNumber)).Items[0];
        var logToKeep = (await ListAsync(adminToken, search: toKeep.ReferenceNumber)).Items[0];

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/notifications")
        {
            Content = JsonContent.Create(new DeleteNotificationLogsDto([logToDelete.Id])),
        };
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.Empty((await ListAsync(adminToken, search: toDelete.ReferenceNumber)).Items);
        Assert.Single((await ListAsync(adminToken, search: toKeep.ReferenceNumber)).Items);
        Assert.Equal(logToKeep.Id, (await ListAsync(adminToken, search: toKeep.ReferenceNumber)).Items[0].Id);
    }

    [Fact]
    public async Task DeleteNotifications_WithEmptyIds_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/notifications")
        {
            Content = JsonContent.Create(new DeleteNotificationLogsDto([])),
        };
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNotifications_WithoutToken_ReturnsUnauthorized()
    {
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/notifications")
        {
            Content = JsonContent.Create(new DeleteNotificationLogsDto([Guid.NewGuid()])),
        };
        var response = await _client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<ReservationRequestDetailDto> CreateAsGuestAsync(Guid hotelId, Guid roomTypeId)
    {
        var dto = new CreateReservationRequestDto(
            hotelId, roomTypeId, "Jane Guest", "jane@example.com", "+90 555 000 00 00",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
            AdultCount: 2, ChildCount: 0, SpecialRequests: null);

        var response = await _client.PostAsJsonAsync("/api/v1/reservation-requests", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReservationRequestDetailDto>())!;
    }

    private async Task<PagedResult<AdminNotificationLogDto>> ListAsync(string adminToken, string search)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/notifications?search={Uri.EscapeDataString(search)}&pageSize=200");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<AdminNotificationLogDto>>())!;
    }

    private async Task<(Guid HotelId, Guid RoomTypeId, string Slug)> CreateHotelWithRoomTypeAsync(
        string adminToken, int availableCount, string? name = null, string? supervisorEmail = null)
    {
        var slug = $"notification-test-{Guid.NewGuid():N}";
        var dto = new AdminHotelUpsertDto(
            Name: name ?? "Notification Test Hotel",
            Slug: slug,
            Description: "Created by a notification-log integration test.",
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
            RoomTypes: [new AdminRoomTypeDto(null, "Standard Room", "Desc", 2, 1000m, "TRY", availableCount, true)],
            Supervisors: supervisorEmail is null ? [] : [new AdminHotelSupervisorDto(null, "Test Supervisor", supervisorEmail, true)],
            AmenitySlugs: []);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/hotels") { Content = JsonContent.Create(dto) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<AdminHotelDetailDto>();

        return (created!.Id, created.RoomTypes[0].Id!.Value, slug);
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
