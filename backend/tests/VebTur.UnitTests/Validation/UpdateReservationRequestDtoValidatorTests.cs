using VebTur.Application.Contracts.Reservations;
using VebTur.Application.Validation;

namespace VebTur.UnitTests.Validation;

public class UpdateReservationRequestDtoValidatorTests
{
    private readonly UpdateReservationRequestDtoValidator _validator = new();

    private static UpdateReservationRequestDto ValidDto() => new(
        RoomTypeId: Guid.NewGuid(),
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
}
