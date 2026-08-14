using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Validation;

namespace VebTur.UnitTests.Validation;

public class CreateReservationRequestDtoValidatorTests
{
    private readonly CreateReservationRequestDtoValidator _validator = new();

    private static CreateReservationRequestDto ValidDto() => new(
        HotelId: Guid.NewGuid(),
        RoomTypeId: Guid.NewGuid(),
        GuestFullName: "Jane Guest",
        GuestEmail: "jane@example.com",
        GuestPhone: "+90 555 000 00 00",
        CheckInDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
        CheckOutDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
        AdultCount: 2,
        ChildCount: 0,
        SpecialRequests: null);

    [Fact]
    public void ValidDto_PassesValidation()
    {
        var result = _validator.Validate(ValidDto());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyGuestFullName_FailsValidation()
    {
        var result = _validator.Validate(ValidDto() with { GuestFullName = "" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "GuestFullName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void InvalidGuestEmail_FailsValidation(string email)
    {
        var result = _validator.Validate(ValidDto() with { GuestEmail = email });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "GuestEmail");
    }

    [Fact]
    public void CheckInDateInThePast_FailsValidation()
    {
        var result = _validator.Validate(ValidDto() with { CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CheckInDate");
    }

    [Fact]
    public void CheckOutDateNotAfterCheckInDate_FailsValidation()
    {
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        var result = _validator.Validate(ValidDto() with { CheckInDate = checkIn, CheckOutDate = checkIn });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CheckOutDate");
    }

    [Fact]
    public void ZeroAdultCount_FailsValidation()
    {
        var result = _validator.Validate(ValidDto() with { AdultCount = 0 });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AdultCount");
    }

    [Fact]
    public void NegativeChildCount_FailsValidation()
    {
        var result = _validator.Validate(ValidDto() with { ChildCount = -1 });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ChildCount");
    }
}
