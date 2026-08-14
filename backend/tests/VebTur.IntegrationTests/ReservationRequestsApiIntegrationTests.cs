using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Auth;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Reservations;
using Microsoft.Extensions.DependencyInjection;

namespace VebTur.IntegrationTests;

/// <summary>
/// End-to-end coverage of the reservation-request workflow: guest create + reference lookup,
/// logged-in customer create/ownership isolation, admin confirm/reject/cancel with the
/// AvailableCount business rule, and customer self-edit resetting a Confirmed reservation back
/// to Pending/Sent (releasing its availability slot).
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class ReservationRequestsApiIntegrationTests
{
    private readonly VebTurWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReservationRequestsApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GuestCreate_ThenLookupByReference_WorksAndComputesPrice()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 3);

        var response = await _client.PostAsJsonAsync("/api/v1/reservation-requests", BuildCreateDto(hotelId, roomTypeId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ReservationRequestDetailDto>();
        Assert.NotNull(created);
        Assert.Equal("Sent", created!.Status);
        Assert.Equal(3000m, created.EstimatedPrice); // 3 nights * 1000

        var lookup = await _client.GetAsync($"/api/v1/reservation-requests/{created.ReferenceNumber}");
        lookup.EnsureSuccessStatusCode();
        var looked = await lookup.Content.ReadFromJsonAsync<ReservationRequestDetailDto>();
        Assert.Equal(created.Id, looked!.Id);
    }

    [Fact]
    public async Task LoggedInCustomerCreate_AppearsInMine_AndIsIsolatedFromOtherCustomers()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 5);
        var customerAToken = await RegisterCustomerAsync();
        var customerBToken = await RegisterCustomerAsync();

        var created = await CreateAsCustomerAsync(customerAToken, hotelId, roomTypeId);

        using var mineRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reservation-requests/mine?pageSize=50");
        mineRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerAToken);
        var minePaged = await (await _client.SendAsync(mineRequest)).Content.ReadFromJsonAsync<PagedResult<ReservationRequestDetailDto>>();
        Assert.Contains(minePaged!.Items, r => r.Id == created.Id);

        using var otherGetRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservation-requests/mine/{created.Id}");
        otherGetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerBToken);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(otherGetRequest)).StatusCode);

        using var otherCancelRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservation-requests/mine/{created.Id}/cancel");
        otherCancelRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerBToken);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(otherCancelRequest)).StatusCode);
    }

    [Fact]
    public async Task Mine_SortByUpdated_OrdersByMostRecentStatusChange_NotCreationOrder()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 5);
        var customerToken = await RegisterCustomerAsync();

        // Created first, but confirmed last below — should sort first under sortByUpdated=true.
        var olderButRecentlyUpdated = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        var newerButUntouched = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, olderButRecentlyUpdated.Id));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reservation-requests/mine?sortByUpdated=true&pageSize=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<ReservationRequestDetailDto>>();

        var ourIds = paged!.Items
            .Where(r => r.Id == olderButRecentlyUpdated.Id || r.Id == newerButUntouched.Id)
            .Select(r => r.Id)
            .ToList();
        Assert.Equal([olderButRecentlyUpdated.Id, newerButUntouched.Id], ourIds);
    }

    [Fact]
    public async Task AdminConfirm_DecrementsAvailableCount_AndBlocksAFurtherConfirmWhenZero()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 1);

        var first = await CreateAsGuestAsync(hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, first.Id));

        var second = await CreateAsGuestAsync(hotelId, roomTypeId);
        using var secondConfirm = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reservation-requests/{second.Id}/confirm");
        secondConfirm.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(secondConfirm)).StatusCode);
    }

    [Fact]
    public async Task CustomerEdit_OfConfirmedReservation_IsRejected_AndDoesNotTouchAvailability()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 1);
        var customerToken = await RegisterCustomerAsync();

        var created = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, created.Id));

        var updateDto = new UpdateReservationRequestDto(
            roomTypeId, created.CheckInDate.AddDays(1), created.CheckOutDate.AddDays(1), created.AdultCount, created.ChildCount, "Late check-in please");
        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/reservation-requests/mine/{created.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        // The Confirmed slot must still be held — a further reservation against the same
        // (availableCount: 1) room type must NOT be confirmable, since the edit never released it.
        var another = await CreateAsGuestAsync(hotelId, roomTypeId);
        using var anotherConfirm = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reservation-requests/{another.Id}/confirm");
        anotherConfirm.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(anotherConfirm)).StatusCode);
    }

    [Fact]
    public async Task CustomerCancel_OfConfirmedReservation_ReleasesAvailableCount()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 1);
        var customerToken = await RegisterCustomerAsync();

        var created = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, created.Id));

        using var cancelRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservation-requests/mine/{created.Id}/cancel");
        cancelRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(cancelRequest)).StatusCode);

        var another = await CreateAsGuestAsync(hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, another.Id));
    }

    [Fact]
    public async Task AdminReject_FromSent_Succeeds_ButNotFromAlreadyConfirmed()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 2);

        var pending = await CreateAsGuestAsync(hotelId, roomTypeId);
        using var rejectRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reservation-requests/{pending.Id}/reject");
        rejectRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(rejectRequest)).StatusCode);

        var confirmed = await CreateAsGuestAsync(hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, confirmed.Id));

        using var rejectConfirmedRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reservation-requests/{confirmed.Id}/reject");
        rejectConfirmedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(rejectConfirmedRequest)).StatusCode);
    }

    [Fact]
    public async Task AdminReservations_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/admin/reservation-requests");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminReservations_WithNonAdminRoleToken_ReturnsForbidden()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var nonAdminToken = jwtTokenService.GenerateToken(Guid.CreateVersion7(), "nobody@example.com", []).Token;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/reservation-requests");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", nonAdminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReservationRequestsMine_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/reservation-requests/mine");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminReservations_SortByHotel_GroupsSameHotelRowsAdjacentlyInNameOrder()
    {
        var adminToken = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (hotelAId, roomTypeAId, _) = await CreateHotelWithRoomTypeAsync(adminToken, 5, $"AAA Sort Test {suffix}");
        var (hotelBId, roomTypeBId, _) = await CreateHotelWithRoomTypeAsync(adminToken, 5, $"ZZZ Sort Test {suffix}");

        // Interleave creation order (B, A, B, A) so a correct sort can't just fall out of insertion order.
        var b1 = await CreateAsGuestAsync(hotelBId, roomTypeBId);
        var a1 = await CreateAsGuestAsync(hotelAId, roomTypeAId);
        var b2 = await CreateAsGuestAsync(hotelBId, roomTypeBId);
        var a2 = await CreateAsGuestAsync(hotelAId, roomTypeAId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/reservation-requests?sort=hotel-asc&pageSize=200");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<AdminReservationSummaryDto>>();

        var ourIds = paged!.Items
            .Where(r => r.Id == a1.Id || r.Id == a2.Id || r.Id == b1.Id || r.Id == b2.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(4, ourIds.Count);
        var aIndices = new[] { ourIds.IndexOf(a1.Id), ourIds.IndexOf(a2.Id) };
        var bIndices = new[] { ourIds.IndexOf(b1.Id), ourIds.IndexOf(b2.Id) };
        Assert.True(aIndices.Max() < bIndices.Min(), "Both AAA-hotel rows should sort before both ZZZ-hotel rows, regardless of creation order.");
    }

    [Fact]
    public async Task AdminReservations_SearchByReference_ReturnsOnlyTheMatchingReservation()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 5);

        var target = await CreateAsGuestAsync(hotelId, roomTypeId);
        await CreateAsGuestAsync(hotelId, roomTypeId);

        // A partial, lowercase search (mirroring the frontend pasting the code without matching case)
        // must still find it via a case-insensitive contains match.
        var partialReference = target.ReferenceNumber[4..].ToLowerInvariant();
        var paged = await SearchAsync(adminToken, partialReference);

        Assert.Single(paged.Items);
        Assert.Equal(target.Id, paged.Items[0].Id);
    }

    [Fact]
    public async Task AdminReservations_SearchByGuestEmail_ReturnsOnlyTheMatchingReservation()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 5);

        var uniqueEmail = $"search-target-{Guid.NewGuid():N}@example.com";
        var createResponse = await _client.PostAsJsonAsync("/api/v1/reservation-requests",
            BuildCreateDto(hotelId, roomTypeId) with { GuestEmail = uniqueEmail });
        createResponse.EnsureSuccessStatusCode();
        var target = (await createResponse.Content.ReadFromJsonAsync<ReservationRequestDetailDto>())!;
        await CreateAsGuestAsync(hotelId, roomTypeId);

        // Proves search isn't reference-only — whatever field the admin types (here, an email
        // fragment) should find the matching reservation, same as searching by reference would.
        var paged = await SearchAsync(adminToken, uniqueEmail[..12]);

        Assert.Single(paged.Items);
        Assert.Equal(target.Id, paged.Items[0].Id);
    }

    [Fact]
    public async Task AdminReservations_MultiStatusFilter_ReturnsReservationsMatchingAnyGivenStatus()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 5);

        var sent = await CreateAsGuestAsync(hotelId, roomTypeId);
        var confirmed = await CreateAsGuestAsync(hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, confirmed.Id));
        var rejected = await CreateAsGuestAsync(hotelId, roomTypeId);
        using var rejectRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reservation-requests/{rejected.Id}/reject");
        rejectRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        (await _client.SendAsync(rejectRequest)).EnsureSuccessStatusCode();

        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/api/v1/admin/reservation-requests?status=Sent&status=Rejected&pageSize=200");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<AdminReservationSummaryDto>>();

        var ids = paged!.Items.Select(r => r.Id).ToList();
        Assert.Contains(sent.Id, ids);
        Assert.Contains(rejected.Id, ids);
        Assert.DoesNotContain(confirmed.Id, ids);
    }

    [Fact]
    public async Task AdminReservations_Delete_RemovesItFromAdminListAndReturnsNotFoundOnRepeat()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 5);
        var reservation = await CreateAsGuestAsync(hotelId, roomTypeId);

        Assert.Equal(HttpStatusCode.NoContent, await DeleteAsync(adminToken, reservation.Id));

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/reservation-requests/{reservation.Id}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(getRequest)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, await DeleteAsync(adminToken, reservation.Id));
    }

    [Fact]
    public async Task AdminReservations_Delete_OfConfirmedReservation_ReleasesAvailableCount()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 1);

        var confirmed = await CreateAsGuestAsync(hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, confirmed.Id));

        Assert.Equal(HttpStatusCode.NoContent, await DeleteAsync(adminToken, confirmed.Id));

        // The slot held by the now-deleted Confirmed reservation must have been released — a fresh
        // reservation against the same (still availableCount: 1) room type must be confirmable again.
        var another = await CreateAsGuestAsync(hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, another.Id));
    }

    [Fact]
    public async Task AdminReservations_Delete_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync($"/api/v1/admin/reservation-requests/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<ReservationRequestDetailDto> CreateAsGuestAsync(Guid hotelId, Guid roomTypeId)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/reservation-requests", BuildCreateDto(hotelId, roomTypeId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReservationRequestDetailDto>())!;
    }

    private async Task<ReservationRequestDetailDto> CreateAsCustomerAsync(string customerToken, Guid hotelId, Guid roomTypeId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservation-requests")
        {
            Content = JsonContent.Create(BuildCreateDto(hotelId, roomTypeId)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReservationRequestDetailDto>())!;
    }

    private async Task<HttpStatusCode> ConfirmAsync(string adminToken, Guid reservationId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reservation-requests/{reservationId}/confirm");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    private async Task<PagedResult<AdminReservationSummaryDto>> SearchAsync(string adminToken, string search)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/reservation-requests?search={Uri.EscapeDataString(search)}&pageSize=200");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<AdminReservationSummaryDto>>())!;
    }

    private async Task<HttpStatusCode> DeleteAsync(string adminToken, Guid reservationId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/reservation-requests/{reservationId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    private static CreateReservationRequestDto BuildCreateDto(Guid hotelId, Guid roomTypeId) => new(
        hotelId, roomTypeId, "Jane Guest", "jane@example.com", "+90 555 000 00 00",
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
        AdultCount: 2, ChildCount: 0, SpecialRequests: null);

    private async Task<(Guid HotelId, Guid RoomTypeId, string Slug)> CreateHotelWithRoomTypeAsync(string adminToken, int availableCount, string? name = null)
    {
        var slug = $"reservation-test-{Guid.NewGuid():N}";
        var dto = new AdminHotelUpsertDto(
            Name: name ?? "Reservation Test Hotel",
            Slug: slug,
            Description: "Created by a reservation integration test.",
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
        var email = $"reservation-guest-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequestDto(email, "Passw0rd123", "Reservation Test Customer"));
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
