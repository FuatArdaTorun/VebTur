using VebTur.Application.Contracts.Admin;
using VebTur.Application.Validation;

namespace VebTur.UnitTests.Validation;

public class AdminHotelUpsertDtoValidatorTests
{
    private readonly AdminHotelUpsertDtoValidator _validator = new();

    private static AdminHotelUpsertDto ValidDto() => new(
        Name: "Test Hotel",
        Slug: "test-hotel",
        Description: "A perfectly valid test hotel.",
        City: "Antalya",
        Country: "Turkey",
        Address: "Some Address 1",
        Latitude: 36.9,
        Longitude: 30.7,
        StarRating: 4,
        OfficialWebsiteUrl: "https://example.com",
        GooglePlaceId: "ChIJabc123",
        PhoneNumber: "+90 242 000 00 00",
        GoogleRating: 4.5m,
        GoogleRatingCount: 100,
        IsActive: true,
        Images: [new AdminHotelImageDto(null, "https://example.com/a.jpg", "Alt", 1)],
        RoomTypes: [new AdminRoomTypeDto(null, "Standard", "Desc", 2, 1000m, "TRY", 5, true)],
        Supervisors: [new AdminHotelSupervisorDto(null, "Jane Doe", "jane@example.com", true)],
        AmenitySlugs: ["wifi"]);

    [Fact]
    public void ValidDto_PassesValidation()
    {
        var result = _validator.Validate(ValidDto());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var result = _validator.Validate(ValidDto() with { Name = "" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("Invalid Slug")]
    [InlineData("UPPERCASE")]
    [InlineData("trailing-")]
    [InlineData("-leading")]
    [InlineData("double--hyphen")]
    public void InvalidSlugFormat_FailsValidation(string slug)
    {
        var result = _validator.Validate(ValidDto() with { Slug = slug });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Slug");
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void LatitudeOutOfRange_FailsValidation(double latitude)
    {
        var result = _validator.Validate(ValidDto() with { Latitude = latitude });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Latitude");
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void LongitudeOutOfRange_FailsValidation(double longitude)
    {
        var result = _validator.Validate(ValidDto() with { Longitude = longitude });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Longitude");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void StarRatingOutOfRange_FailsValidation(int starRating)
    {
        var result = _validator.Validate(ValidDto() with { StarRating = starRating });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void StarRatingNull_IsAllowed()
    {
        var result = _validator.Validate(ValidDto() with { StarRating = null });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NegativeRoomPrice_FailsValidation()
    {
        var dto = ValidDto() with { RoomTypes = [new AdminRoomTypeDto(null, "Standard", "Desc", 2, -100m, "TRY", 5, true)] };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ZeroCapacityRoomType_FailsValidation()
    {
        var dto = ValidDto() with { RoomTypes = [new AdminRoomTypeDto(null, "Standard", "Desc", 0, 1000m, "TRY", 5, true)] };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void InvalidSupervisorEmail_FailsValidation()
    {
        var dto = ValidDto() with { Supervisors = [new AdminHotelSupervisorDto(null, "Jane Doe", "not-an-email", true)] };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void InvalidImageUrl_FailsValidation()
    {
        var dto = ValidDto() with { Images = [new AdminHotelImageDto(null, "not-a-url", null, 1)] };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void InvalidOfficialWebsiteUrl_FailsValidation()
    {
        var result = _validator.Validate(ValidDto() with { OfficialWebsiteUrl = "not-a-url" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NullOfficialWebsiteUrl_IsAllowed()
    {
        var result = _validator.Validate(ValidDto() with { OfficialWebsiteUrl = null });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void GoogleRatingOutOfRange_FailsValidation(decimal rating)
    {
        var result = _validator.Validate(ValidDto() with { GoogleRating = rating });
        Assert.False(result.IsValid);
    }
}
