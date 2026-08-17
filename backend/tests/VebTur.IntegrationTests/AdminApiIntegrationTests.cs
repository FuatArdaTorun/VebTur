using System.Net;
using System.Net.Http.Json;
using VebTur.Application.Auth;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Hotels;
using Microsoft.Extensions.DependencyInjection;

namespace VebTur.IntegrationTests;

/// <summary>
/// Exercises the real API pipeline end-to-end against a real Postgres database — auth, role
/// enforcement, and the full admin hotel CRUD lifecycle, checked against the public read API too
/// so a change made through the admin surface is verified to actually reach real visitors.
/// All tests share one <see cref="VebTurWebApplicationFactory"/> (xUnit runs tests within a
/// class sequentially by default), so each test uses a GUID-suffixed slug to avoid colliding
/// with the others.
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class AdminApiIntegrationTests
{
    private readonly VebTurWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenWithAdminRole()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto(VebTurWebApplicationFactory.AdminEmail, VebTurWebApplicationFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Contains("Admin", body.Roles);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto(VebTurWebApplicationFactory.AdminEmail, "definitely-wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto("nobody@vebtur-integration-tests.local", "whatever"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminHotels_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/admin/hotels");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminHotels_WithNonAdminRoleToken_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/hotels");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GetNonAdminToken());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PublicHotelsList_IncludesSeededHotels()
    {
        var response = await _client.GetAsync("/api/v1/hotels?pageSize=50");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<PagedResult<HotelSummaryDto>>();
        Assert.NotNull(body);
        Assert.True(body!.TotalCount >= 18, $"Expected at least the 18 seeded hotels, got {body.TotalCount}.");
    }

    [Fact]
    public async Task PublicHotelsList_SearchFiltersByNameOrCity_CaseInsensitively()
    {
        var token = await GetAdminTokenAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var uniqueName = $"Zzyzx Search Test Hotel {uniqueSuffix}";
        var created = await PostHotelAsync(token, BuildMinimalHotel($"search-test-{uniqueSuffix}") with { Name = uniqueName });
        created.EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/v1/hotels?search={Uri.EscapeDataString(uniqueName[..10].ToLowerInvariant())}&pageSize=50");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PagedResult<HotelSummaryDto>>();

        Assert.Contains(body!.Items, h => h.Name == uniqueName);
        Assert.All(body.Items, h => Assert.Contains("zzyzx", h.Name.ToLowerInvariant()));
    }

    [Fact]
    public async Task CreateHotel_WithDuplicateSlug_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        var slug = $"dup-slug-test-{Guid.NewGuid():N}";

        var first = await PostHotelAsync(token, BuildMinimalHotel(slug));
        first.EnsureSuccessStatusCode();

        var second = await PostHotelAsync(token, BuildMinimalHotel(slug));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task CreateHotel_WithUnknownAmenitySlug_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        var dto = BuildMinimalHotel($"unknown-amenity-test-{Guid.NewGuid():N}") with { AmenitySlugs = ["not-a-real-amenity-slug"] };

        var response = await PostHotelAsync(token, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateHotel_WithInvalidStructuralData_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        var dto = BuildMinimalHotel($"invalid-test-{Guid.NewGuid():N}") with { Name = "", Latitude = 999 };

        var response = await PostHotelAsync(token, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HotelLifecycle_CreateUpdateDeactivateReactivate_WorksEndToEndAndReflectsPublicly()
    {
        var token = await GetAdminTokenAsync();
        var slug = $"lifecycle-test-{Guid.NewGuid():N}";

        // Create
        var createDto = new AdminHotelUpsertDto(
            Name: "Lifecycle Test Hotel",
            Slug: slug,
            Description: "Created by an integration test.",
            City: "Antalya",
            Country: "Turkey",
            Address: "Test Address 1",
            Latitude: 36.9,
            Longitude: 30.7,
            StarRating: 4,
            OfficialWebsiteUrl: "https://example.com",
            GooglePlaceId: "ChIJoriginal",
            PhoneNumber: "+90 242 000 00 00",
            GoogleRating: 4.5m,
            GoogleRatingCount: 100,
            IsActive: true,
            Images: [new AdminHotelImageDto(null, "https://example.com/photo1.jpg", "Exterior", 1)],
            RoomTypes: [new AdminRoomTypeDto(null, "Standard Room", "Desc", 2, 1000m, "TRY", 5, true)],
            Supervisors: [new AdminHotelSupervisorDto(null, "Jane Doe", "jane@example.com", true)],
            AmenitySlugs: ["wifi", "pool"]);

        var createResponse = await PostHotelAsync(token, createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminHotelDetailDto>();
        Assert.NotNull(created);

        // Visible publicly
        var publicList = await _client.GetFromJsonAsync<PagedResult<HotelSummaryDto>>("/api/v1/hotels?pageSize=100");
        Assert.Contains(publicList!.Items, h => h.Slug == slug);

        // Update: rename, reprice, keep+add image, drop the supervisor, change GooglePlaceId
        var existingImageId = created!.Images[0].Id;
        var updateDto = new AdminHotelUpsertDto(
            Name: "Lifecycle Test Hotel Updated",
            Slug: created.Slug,
            Description: created.Description,
            City: created.City,
            Country: created.Country,
            Address: created.Address,
            Latitude: created.Latitude,
            Longitude: created.Longitude,
            StarRating: created.StarRating,
            OfficialWebsiteUrl: created.OfficialWebsiteUrl,
            GooglePlaceId: "ChIJupdated",
            PhoneNumber: created.PhoneNumber,
            GoogleRating: created.GoogleRating,
            GoogleRatingCount: created.GoogleRatingCount,
            IsActive: created.IsActive,
            Images:
            [
                new AdminHotelImageDto(existingImageId, "https://example.com/photo1-updated.jpg", "Exterior updated", 1),
                new AdminHotelImageDto(null, "https://example.com/photo2.jpg", "Pool", 2),
            ],
            RoomTypes: [new AdminRoomTypeDto(null, "Deluxe Room", "Desc", 3, 2000m, "TRY", 5, true)],
            Supervisors: [],
            AmenitySlugs: ["wifi"]);

        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/hotels/{created.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminHotelDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Lifecycle Test Hotel Updated", updated!.Name);
        Assert.Equal("ChIJupdated", updated.GooglePlaceId);
        Assert.Equal(2, updated.Images.Count);
        Assert.Single(updated.RoomTypes);
        Assert.Equal("Deluxe Room", updated.RoomTypes[0].Name);
        Assert.Empty(updated.Supervisors);
        Assert.Equal(["wifi"], updated.AmenitySlugs);
        Assert.True(updated.UpdatedAtUtc > updated.CreatedAtUtc);

        // Reflected publicly
        var publicDetail = await _client.GetFromJsonAsync<HotelDetailDto>($"/api/v1/hotels/{slug}");
        Assert.Equal("Lifecycle Test Hotel Updated", publicDetail!.Name);

        // Deactivate
        using var deactivateRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/hotels/{created.Id}");
        deactivateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var deactivateResponse = await _client.SendAsync(deactivateRequest);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        // Gone from public surface
        var publicDetailAfterDeactivate = await _client.GetAsync($"/api/v1/hotels/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, publicDetailAfterDeactivate.StatusCode);

        var publicListAfterDeactivate = await _client.GetFromJsonAsync<PagedResult<HotelSummaryDto>>("/api/v1/hotels?pageSize=100");
        Assert.DoesNotContain(publicListAfterDeactivate!.Items, h => h.Slug == slug);

        // Still visible to admin with isActive=false
        using var adminInactiveListRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/hotels?isActive=false&pageSize=100");
        adminInactiveListRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var adminInactiveListResponse = await _client.SendAsync(adminInactiveListRequest);
        var adminInactiveList = await adminInactiveListResponse.Content.ReadFromJsonAsync<PagedResult<AdminHotelSummaryDto>>();
        Assert.Contains(adminInactiveList!.Items, h => h.Slug == slug);

        // Reactivate
        using var reactivateRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/hotels/{created.Id}/reactivate");
        reactivateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var reactivateResponse = await _client.SendAsync(reactivateRequest);
        Assert.Equal(HttpStatusCode.NoContent, reactivateResponse.StatusCode);

        // Reappears publicly
        var publicListAfterReactivate = await _client.GetFromJsonAsync<PagedResult<HotelSummaryDto>>("/api/v1/hotels?pageSize=100");
        Assert.Contains(publicListAfterReactivate!.Items, h => h.Slug == slug);

        // Permanent delete — irreversible, distinct from deactivate above
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/hotels/{created.Id}/permanent");
        deleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Gone from the public surface
        var publicDetailAfterDelete = await _client.GetAsync($"/api/v1/hotels/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, publicDetailAfterDelete.StatusCode);

        // Gone from the admin surface too (unlike deactivate, no isActive=false record survives)
        using var adminAfterDeleteRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/hotels?isActive=false&pageSize=100");
        adminAfterDeleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var adminAfterDeleteResponse = await _client.SendAsync(adminAfterDeleteRequest);
        var adminListAfterDelete = await adminAfterDeleteResponse.Content.ReadFromJsonAsync<PagedResult<AdminHotelSummaryDto>>();
        Assert.DoesNotContain(adminListAfterDelete!.Items, h => h.Slug == slug);

        // Deleting again is a no-op 404, not an error
        using var secondDeleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/hotels/{created.Id}/permanent");
        secondDeleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var secondDeleteResponse = await _client.SendAsync(secondDeleteRequest);
        Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task PermanentDelete_OfHotelWithReservationHistory_ReturnsBadRequest_AndLeavesHotelIntact()
    {
        var token = await GetAdminTokenAsync();
        var slug = $"delete-guard-test-{Guid.NewGuid():N}";
        var createDto = BuildMinimalHotel(slug) with
        {
            RoomTypes = [new AdminRoomTypeDto(null, "Standard Room", "Desc", 2, 1000m, "TRY", 5, true)],
        };
        var createResponse = await PostHotelAsync(token, createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminHotelDetailDto>();

        var reservationDto = new CreateReservationRequestDto(
            created!.Id, created.RoomTypes[0].Id!.Value, "Guest Name", "guest@example.com", "+90 555 000 00 00",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
            AdultCount: 2, ChildCount: 0, SpecialRequests: null);
        (await _client.PostAsJsonAsync("/api/v1/reservation-requests", reservationDto)).EnsureSuccessStatusCode();

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/hotels/{created.Id}/permanent");
        deleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await _client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        // Untouched — still visible on the public surface.
        var publicDetail = await _client.GetAsync($"/api/v1/hotels/{slug}");
        Assert.Equal(HttpStatusCode.OK, publicDetail.StatusCode);
    }

    [Fact]
    public async Task AdminHotelsList_ReflectsHasReservationHistory_TrueOnlyForHotelsWithReservations()
    {
        var token = await GetAdminTokenAsync();
        var withHistorySlug = $"has-history-test-{Guid.NewGuid():N}";
        var withHistoryDto = BuildMinimalHotel(withHistorySlug) with
        {
            RoomTypes = [new AdminRoomTypeDto(null, "Standard Room", "Desc", 2, 1000m, "TRY", 5, true)],
        };
        var withHistoryResponse = await PostHotelAsync(token, withHistoryDto);
        var withHistory = await withHistoryResponse.Content.ReadFromJsonAsync<AdminHotelDetailDto>();

        var reservationDto = new CreateReservationRequestDto(
            withHistory!.Id, withHistory.RoomTypes[0].Id!.Value, "Guest Name", "guest@example.com", "+90 555 000 00 00",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
            AdultCount: 2, ChildCount: 0, SpecialRequests: null);
        (await _client.PostAsJsonAsync("/api/v1/reservation-requests", reservationDto)).EnsureSuccessStatusCode();

        var withoutHistorySlug = $"no-history-test-{Guid.NewGuid():N}";
        var withoutHistoryResponse = await PostHotelAsync(token, BuildMinimalHotel(withoutHistorySlug));
        var withoutHistory = await withoutHistoryResponse.Content.ReadFromJsonAsync<AdminHotelDetailDto>();

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/hotels?pageSize=200");
        listRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var listResponse = await _client.SendAsync(listRequest);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<AdminHotelSummaryDto>>();

        Assert.True(list!.Items.Single(h => h.Id == withHistory.Id).HasReservationHistory);
        Assert.False(list.Items.Single(h => h.Id == withoutHistory!.Id).HasReservationHistory);
    }

    [Fact]
    public async Task BulkDeleteHotelsPermanently_RemovesEligibleHotels_AndSkipsOnesWithReservationHistory()
    {
        var token = await GetAdminTokenAsync();

        var blockedSlug = $"bulk-blocked-test-{Guid.NewGuid():N}";
        var blockedDto = BuildMinimalHotel(blockedSlug) with
        {
            RoomTypes = [new AdminRoomTypeDto(null, "Standard Room", "Desc", 2, 1000m, "TRY", 5, true)],
        };
        var blockedResponse = await PostHotelAsync(token, blockedDto);
        var blocked = await blockedResponse.Content.ReadFromJsonAsync<AdminHotelDetailDto>();
        var reservationDto = new CreateReservationRequestDto(
            blocked!.Id, blocked.RoomTypes[0].Id!.Value, "Guest Name", "guest@example.com", "+90 555 000 00 00",
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
            AdultCount: 2, ChildCount: 0, SpecialRequests: null);
        (await _client.PostAsJsonAsync("/api/v1/reservation-requests", reservationDto)).EnsureSuccessStatusCode();

        var eligibleSlug = $"bulk-eligible-test-{Guid.NewGuid():N}";
        var eligibleResponse = await PostHotelAsync(token, BuildMinimalHotel(eligibleSlug));
        var eligible = await eligibleResponse.Content.ReadFromJsonAsync<AdminHotelDetailDto>();

        Assert.Equal(HttpStatusCode.NoContent, await BulkDeleteHotelsAsync(token, [blocked.Id, eligible!.Id]));

        using var getBlocked = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/hotels/{blocked.Id}");
        getBlocked.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getBlocked)).StatusCode);

        using var getEligible = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/hotels/{eligible.Id}");
        getEligible.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(getEligible)).StatusCode);
    }

    [Fact]
    public async Task BulkDeleteHotelsPermanently_WithEmptyIds_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();

        Assert.Equal(HttpStatusCode.BadRequest, await BulkDeleteHotelsAsync(token, []));
    }

    private async Task<HttpStatusCode> BulkDeleteHotelsAsync(string token, Guid[] ids)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/admin/hotels/permanent")
        {
            Content = JsonContent.Create(new DeleteHotelsDto(ids)),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
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

    /// <summary>A validly-signed token for a user with no roles — proves [Authorize(Roles="Admin")]
    /// actually checks the role claim rather than merely "is this token valid."</summary>
    private string GetNonAdminToken()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtTokenService.GenerateToken(Guid.CreateVersion7(), "nobody@example.com", []).Token;
    }

    private async Task<HttpResponseMessage> PostHotelAsync(string token, AdminHotelUpsertDto dto)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/hotels")
        {
            Content = JsonContent.Create(dto),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private static AdminHotelUpsertDto BuildMinimalHotel(string slug) => new(
        Name: "Minimal Test Hotel",
        Slug: slug,
        Description: "Minimal valid hotel for negative-path tests.",
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
}
