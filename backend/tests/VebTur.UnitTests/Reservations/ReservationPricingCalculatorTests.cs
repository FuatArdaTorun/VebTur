using VebTur.Application.Reservations;

namespace VebTur.UnitTests.Reservations;

public class ReservationPricingCalculatorTests
{
    [Theory]
    [InlineData(1, 1000, 1000)]
    [InlineData(3, 1000, 3000)]
    [InlineData(7, 250.50, 1753.50)]
    public void CalculateEstimatedPrice_MultipliesNightsByBaseNightlyPrice(int nights, decimal baseNightlyPrice, decimal expected)
    {
        var checkIn = new DateOnly(2026, 9, 1);
        var checkOut = checkIn.AddDays(nights);

        var result = ReservationPricingCalculator.CalculateEstimatedPrice(checkIn, checkOut, baseNightlyPrice);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateEstimatedPrice_SameDayCheckInAndCheckOut_ReturnsZero()
    {
        var day = new DateOnly(2026, 9, 1);

        var result = ReservationPricingCalculator.CalculateEstimatedPrice(day, day, 1000m);

        Assert.Equal(0m, result);
    }
}
