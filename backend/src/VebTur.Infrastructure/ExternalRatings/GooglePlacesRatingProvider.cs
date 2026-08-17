using System.Net.Http.Json;
using System.Text.Json.Serialization;
using VebTur.Application.ExternalRatings;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VebTur.Infrastructure.ExternalRatings;

/// <summary>
/// Live rating via the Places API (New) Place Details endpoint.
/// Requests only <c>rating</c>/<c>userRatingCount</c> — enough for the star
/// rating and review count shown on the hotel detail page, without pulling in the
/// Enterprise+Atmosphere-tier <c>reviews</c> field (review text/snippets are deliberately not
/// fetched or cached here yet). Every failure mode (no key configured, hotel has no
/// GooglePlaceId, network error, non-success response, unparsable body) returns null rather than
/// throwing, so <see cref="ExternalRatingService"/> can fall back to
/// <see cref="ManualExternalRatingProvider"/> and the hotel detail page never breaks because of
/// this external dependency.
/// </summary>
public class GooglePlacesRatingProvider(
    HttpClient httpClient,
    IOptions<GooglePlacesOptions> options,
    ILogger<GooglePlacesRatingProvider> logger) : IExternalRatingProvider
{
    public ExternalProvider Provider => ExternalProvider.Google;

    public async Task<ExternalRatingResult?> TryGetRatingAsync(Hotel hotel, CancellationToken cancellationToken)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(hotel.GooglePlaceId))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/places/{hotel.GooglePlaceId}");
            request.Headers.Add("X-Goog-Api-Key", apiKey);
            request.Headers.Add("X-Goog-FieldMask", "rating,userRatingCount");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Google Places lookup for hotel {HotelId} returned {StatusCode}; falling back to demo data.",
                    hotel.Id, response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<PlaceDetailsResponse>(cancellationToken);
            if (body?.Rating is not { } rating || body.UserRatingCount is not { } reviewCount)
            {
                return null;
            }

            return new ExternalRatingResult(Convert.ToDecimal(rating), MaximumRating: 5m, reviewCount);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Google Places lookup for hotel {HotelId} failed; falling back to demo data.", hotel.Id);
            return null;
        }
    }

    private record PlaceDetailsResponse(
        [property: JsonPropertyName("rating")] double? Rating,
        [property: JsonPropertyName("userRatingCount")] int? UserRatingCount);
}
