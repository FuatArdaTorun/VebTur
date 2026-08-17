using System.Net;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.ExternalRatings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace VebTur.UnitTests.ExternalRatings;

public class GooglePlacesRatingProviderTests
{
    [Fact]
    public void Provider_IsGoogle()
    {
        var provider = BuildProvider(apiKey: null, handler: new StubHandler(_ => throw new InvalidOperationException("should not be called")));

        Assert.Equal(ExternalProvider.Google, provider.Provider);
    }

    [Fact]
    public async Task TryGetRatingAsync_WithoutApiKeyConfigured_ReturnsNull_WithoutCallingOut()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("should not have made an HTTP call"));
        var provider = BuildProvider(apiKey: null, handler);
        var hotel = BuildHotel(googlePlaceId: "place-123");

        var result = await provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetRatingAsync_WithoutGooglePlaceIdOnHotel_ReturnsNull_WithoutCallingOut()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("should not have made an HTTP call"));
        var provider = BuildProvider(apiKey: "test-key", handler);
        var hotel = BuildHotel(googlePlaceId: null);

        var result = await provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetRatingAsync_WithSuccessfulResponse_ReturnsMappedResult()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"rating":4.5,"userRatingCount":1234}"""),
        });
        var provider = BuildProvider(apiKey: "test-key", handler);
        var hotel = BuildHotel(googlePlaceId: "place-123");

        var result = await provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(4.5m, result!.Rating);
        Assert.Equal(5m, result.MaximumRating);
        Assert.Equal(1234, result.ReviewCount);
    }

    [Fact]
    public async Task TryGetRatingAsync_SendsApiKeyAndFieldMaskHeaders()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"rating":4.0,"userRatingCount":10}""") };
        });
        var provider = BuildProvider(apiKey: "test-key", handler);
        var hotel = BuildHotel(googlePlaceId: "place-abc");

        await provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("test-key", captured!.Headers.GetValues("X-Goog-Api-Key").Single());
        Assert.Equal("rating,userRatingCount", captured.Headers.GetValues("X-Goog-FieldMask").Single());
        Assert.EndsWith("v1/places/place-abc", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task TryGetRatingAsync_WithNonSuccessStatusCode_ReturnsNull()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var provider = BuildProvider(apiKey: "test-key", handler);
        var hotel = BuildHotel(googlePlaceId: "place-123");

        var result = await provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetRatingAsync_WhenHttpCallThrows_ReturnsNull()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("network down"));
        var provider = BuildProvider(apiKey: "test-key", handler);
        var hotel = BuildHotel(googlePlaceId: "place-123");

        var result = await provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetRatingAsync_WithResponseMissingFields_ReturnsNull()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        var provider = BuildProvider(apiKey: "test-key", handler);
        var hotel = BuildHotel(googlePlaceId: "place-123");

        var result = await provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.Null(result);
    }

    private static GooglePlacesRatingProvider BuildProvider(string? apiKey, HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://places.googleapis.com/") };
        var options = Options.Create(new GooglePlacesOptions { ApiKey = apiKey });
        return new GooglePlacesRatingProvider(httpClient, options, NullLogger<GooglePlacesRatingProvider>.Instance);
    }

    private static Hotel BuildHotel(string? googlePlaceId) => new()
    {
        Name = "Test Hotel",
        Slug = "test-hotel",
        Description = "Desc",
        City = "Antalya",
        Country = "Turkey",
        Address = "Test Address",
        GooglePlaceId = googlePlaceId,
    };

    private class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
