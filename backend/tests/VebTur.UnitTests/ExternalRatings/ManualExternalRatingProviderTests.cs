using VebTur.Domain.Entities;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.ExternalRatings;
using Xunit;

namespace VebTur.UnitTests.ExternalRatings;

public class ManualExternalRatingProviderTests
{
    private readonly ManualExternalRatingProvider _provider = new();

    [Fact]
    public void Provider_IsManual()
    {
        Assert.Equal(ExternalProvider.Manual, _provider.Provider);
    }

    [Fact]
    public async Task TryGetRatingAsync_WithManuallyCapturedGoogleRating_ReturnsIt()
    {
        var hotel = BuildHotel(googleRating: 4.7m, googleRatingCount: 4158);

        var result = await _provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(4.7m, result!.Rating);
        Assert.Equal(5m, result.MaximumRating);
        Assert.Equal(4158, result.ReviewCount);
    }

    [Theory]
    [InlineData(null, 100)]
    [InlineData(4.5, null)]
    [InlineData(null, null)]
    public async Task TryGetRatingAsync_WithoutBothFieldsCaptured_ReturnsNull(double? rating, int? reviewCount)
    {
        var hotel = BuildHotel(rating is null ? null : Convert.ToDecimal(rating), reviewCount);

        var result = await _provider.TryGetRatingAsync(hotel, CancellationToken.None);

        Assert.Null(result);
    }

    private static Hotel BuildHotel(decimal? googleRating, int? googleRatingCount) => new()
    {
        Name = "Test Hotel",
        Slug = "test-hotel",
        Description = "Desc",
        City = "Antalya",
        Country = "Turkey",
        Address = "Test Address",
        GoogleRating = googleRating,
        GoogleRatingCount = googleRatingCount,
    };
}
