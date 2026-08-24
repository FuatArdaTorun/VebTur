using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Application.Contracts.Auth;
using VebTur.Application.Contracts.Hotels;
using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Contracts.Reviews;

namespace VebTur.IntegrationTests;

/// <summary>
/// End-to-end coverage of the customer-facing review flow: eligibility (own + Confirmed +
/// not-already-reviewed), the hotel's aggregate customer rating, and that reviews are hotel-scoped.
/// </summary>
[Collection(VebTurApiCollection.Name)]
public class ReviewsApiIntegrationTests
{
    private readonly VebTurWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReviewsApiIntegrationTests(VebTurWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateReview_ForOwnConfirmedReservation_Succeeds_AndAppearsInPublicList_AndUpdatesCustomerRating()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();

        var reservation = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        Assert.Equal(HttpStatusCode.NoContent, await ConfirmAsync(adminToken, reservation.Id));

        var review = await PostReviewAsync(customerToken, hotelId, new CreateReviewDto(reservation.Id, 5, "Fantastic stay, would come back."));

        Assert.Equal(5, review.Rating);
        Assert.Equal("Fantastic stay, would come back.", review.Comment);

        var list = await GetReviewsAsync(hotelId);
        Assert.Contains(list.Reviews, r => r.Id == review.Id);

        var hotel = await _client.GetFromJsonAsync<HotelDetailDto>($"/api/v1/hotels/{hotelId}");
        Assert.Equal(5m, hotel!.CustomerRating);
        Assert.Equal(1, hotel.CustomerReviewCount);

        // The list/summary endpoint (the hotel card on /hotels and the homepage) carries the same
        // aggregate, not just the detail page.
        var listed = await _client.GetFromJsonAsync<PagedResult<HotelSummaryDto>>(
            $"/api/v1/hotels?search={Uri.EscapeDataString("Review Test Hotel")}&pageSize=50");
        var summary = listed!.Items.Single(h => h.Id == hotelId);
        Assert.Equal(5m, summary.CustomerRating);
        Assert.Equal(1, summary.CustomerReviewCount);
    }

    [Fact]
    public async Task CreateReview_WithoutComment_IsAllowed()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();

        var reservation = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        await ConfirmAsync(adminToken, reservation.Id);

        var review = await PostReviewAsync(customerToken, hotelId, new CreateReviewDto(reservation.Id, 4, null));

        Assert.Equal(4, review.Rating);
        Assert.Null(review.Comment);
    }

    [Fact]
    public async Task CreateReview_ForAnotherCustomersReservation_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken);
        var ownerToken = await RegisterCustomerAsync();
        var otherToken = await RegisterCustomerAsync();

        var reservation = await CreateAsCustomerAsync(ownerToken, hotelId, roomTypeId);
        await ConfirmAsync(adminToken, reservation.Id);

        var response = await SendReviewAsync(otherToken, hotelId, new CreateReviewDto(reservation.Id, 5, "Not mine to review."));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReview_ForAwaitingApprovalReservation_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();

        var reservation = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        // Deliberately not confirmed.

        var response = await SendReviewAsync(customerToken, hotelId, new CreateReviewDto(reservation.Id, 5, "Too soon."));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReview_Twice_ForSameReservation_ReturnsBadRequest_OnSecondAttempt()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();

        var reservation = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        await ConfirmAsync(adminToken, reservation.Id);

        var first = await SendReviewAsync(customerToken, hotelId, new CreateReviewDto(reservation.Id, 4, "First."));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await SendReviewAsync(customerToken, hotelId, new CreateReviewDto(reservation.Id, 2, "Second attempt."));
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task GetReviews_WhenAuthenticated_ListsMyReviewableReservations_AndDropsOneOnceReviewed()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, roomTypeId, _) = await CreateHotelWithRoomTypeAsync(adminToken);
        var customerToken = await RegisterCustomerAsync();

        var reservation = await CreateAsCustomerAsync(customerToken, hotelId, roomTypeId);
        await ConfirmAsync(adminToken, reservation.Id);

        var beforeReview = await GetReviewsAsync(hotelId, customerToken);
        Assert.Contains(beforeReview.MyReviewableReservations, r => r.ReservationRequestId == reservation.Id);

        await PostReviewAsync(customerToken, hotelId, new CreateReviewDto(reservation.Id, 5, "Great!"));

        var afterReview = await GetReviewsAsync(hotelId, customerToken);
        Assert.DoesNotContain(afterReview.MyReviewableReservations, r => r.ReservationRequestId == reservation.Id);
    }

    [Fact]
    public async Task GetReviews_WhenNotAuthenticated_ReturnsEmptyReviewableReservations()
    {
        var adminToken = await GetAdminTokenAsync();
        var (hotelId, _, _) = await CreateHotelWithRoomTypeAsync(adminToken);

        var result = await GetReviewsAsync(hotelId);
        Assert.Empty(result.MyReviewableReservations);
    }

    private async Task<ReviewDto> PostReviewAsync(string customerToken, Guid hotelId, CreateReviewDto dto)
    {
        var response = await SendReviewAsync(customerToken, hotelId, dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReviewDto>())!;
    }

    private async Task<HttpResponseMessage> SendReviewAsync(string customerToken, Guid hotelId, CreateReviewDto dto)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/hotels/{hotelId}/reviews")
        {
            Content = JsonContent.Create(dto),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        return await _client.SendAsync(request);
    }

    private async Task<HotelReviewsResponseDto> GetReviewsAsync(Guid hotelId, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/hotels/{hotelId}/reviews");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HotelReviewsResponseDto>())!;
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

    private static CreateReservationRequestDto BuildCreateDto(Guid hotelId, Guid roomTypeId) => new(
        hotelId, roomTypeId, "Jane Guest", "jane@example.com", "+90 555 000 00 00",
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
        AdultCount: 2, ChildCount: 0, SpecialRequests: null);

    private async Task<(Guid HotelId, Guid RoomTypeId, string Slug)> CreateHotelWithRoomTypeAsync(string adminToken)
    {
        var slug = $"review-test-{Guid.NewGuid():N}";
        var dto = new AdminHotelUpsertDto(
            Name: "Review Test Hotel",
            Slug: slug,
            Description: "Created by a review integration test.",
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
        var email = $"review-customer-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequestDto(email, "Passw0rd123", "Review Test Customer"));
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
