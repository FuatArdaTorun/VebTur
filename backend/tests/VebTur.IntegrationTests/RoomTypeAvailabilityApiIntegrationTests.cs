using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Contracts.Reservations;

namespace VebTur.IntegrationTests;

/// <summary>
/// Covers the reconstructed-capacity math in RoomTypeAvailabilityService: only Confirmed
/// reservations count, the checkout night itself isn't occupied, capacity above 1 needs that many
/// overlapping Confirmed stays, and releasing a Confirmed reservation frees the date back up.
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class RoomTypeAvailabilityApiIntegrationTests
{
    private readonly VebTurWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RoomTypeAvailabilityApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnknownRoomType_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/hotels/room-types/{Guid.NewGuid()}/booked-dates");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NoConfirmedReservations_ReturnsNoBookedDates()
    {
        var adminToken = await GetAdminTokenAsync();
        var (_, roomTypeId) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 2);

        var result = await GetAvailabilityAsync(roomTypeId);

        Assert.Empty(result.FullyBookedDates);
    }

    [Fact]
    public async Task SingleConfirmedReservation_AtCapacityOne_BooksItsNights_ButNotTheCheckoutDay()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 1);
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var checkOut = checkIn.AddDays(3);

        var reservation = await CreateAsGuestAsync(hotelId, roomTypeId, checkIn, checkOut);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, reservation.Id));

        var result = await GetAvailabilityAsync(roomTypeId);

        Assert.Contains(checkIn, result.FullyBookedDates);
        Assert.Contains(checkIn.AddDays(1), result.FullyBookedDates);
        Assert.Contains(checkIn.AddDays(2), result.FullyBookedDates);
        Assert.DoesNotContain(checkOut, result.FullyBookedDates);
        Assert.DoesNotContain(checkIn.AddDays(-1), result.FullyBookedDates);
    }

    [Fact]
    public async Task AwaitingApprovalReservation_DoesNotBookAnyDates()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 1);
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20);
        var checkOut = checkIn.AddDays(2);

        await CreateAsGuestAsync(hotelId, roomTypeId, checkIn, checkOut); // never confirmed

        var result = await GetAvailabilityAsync(roomTypeId);

        Assert.DoesNotContain(checkIn, result.FullyBookedDates);
    }

    [Fact]
    public async Task CapacityTwo_NeedsTwoOverlappingConfirmedReservations_ToBookTheDate()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 2);
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var checkOut = checkIn.AddDays(2);

        var first = await CreateAsGuestAsync(hotelId, roomTypeId, checkIn, checkOut);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, first.Id));
        Assert.DoesNotContain(checkIn, (await GetAvailabilityAsync(roomTypeId)).FullyBookedDates);

        var second = await CreateAsGuestAsync(hotelId, roomTypeId, checkIn, checkOut);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, second.Id));

        Assert.Contains(checkIn, (await GetAvailabilityAsync(roomTypeId)).FullyBookedDates);
    }

    [Fact]
    public async Task CancellingAConfirmedReservation_FreesItsDatesBackUp()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId) = await CreateHotelWithRoomTypeAsync(adminToken, availableCount: 1);
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(40);
        var checkOut = checkIn.AddDays(2);

        var reservation = await CreateAsGuestAsync(hotelId, roomTypeId, checkIn, checkOut);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, reservation.Id));
        Assert.Contains(checkIn, (await GetAvailabilityAsync(roomTypeId)).FullyBookedDates);

        using var cancelRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reservation-requests/{reservation.Id}/cancel");
        cancelRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(cancelRequest)).StatusCode);

        Assert.DoesNotContain(checkIn, (await GetAvailabilityAsync(roomTypeId)).FullyBookedDates);
    }

    private async Task<RoomTypeAvailabilityDto> GetAvailabilityAsync(Guid roomTypeId)
    {
        var response = await _client.GetAsync($"/api/v1/hotels/room-types/{roomTypeId}/booked-dates");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RoomTypeAvailabilityDto>())!;
    }

    private async Task<ReservationRequestDetailDto> CreateAsGuestAsync(Guid hotelId, Guid roomTypeId, DateOnly checkIn, DateOnly checkOut)
    {
        var dto = new CreateReservationRequestDto(
            hotelId, roomTypeId, "Jane Guest", "jane@example.com", "+90 555 000 00 00",
            checkIn, checkOut, AdultCount: 2, ChildCount: 0, SpecialRequests: null);

        var response = await _client.PostAsJsonAsync("/api/v1/reservation-requests", dto);
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

    private async Task<(Guid HotelId, Guid RoomTypeId)> CreateHotelWithRoomTypeAsync(string adminToken, int availableCount)
    {
        var dto = new AdminHotelUpsertDto(
            Name: "Availability Test Hotel",
            Slug: $"availability-test-{Guid.NewGuid():N}",
            Description: "Created by a room-type availability integration test.",
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

        return (created!.Id, created.RoomTypes[0].Id!.Value);
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
