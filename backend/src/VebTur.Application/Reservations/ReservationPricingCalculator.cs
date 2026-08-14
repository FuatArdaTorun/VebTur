namespace VebTur.Application.Reservations;

/// <summary>Pulled out as a pure, static calculator so pricing logic is unit-testable in isolation.</summary>
public static class ReservationPricingCalculator
{
    /// <summary>Nights × base nightly price. No live rate integration, so this is the sole pricing authority.</summary>
    public static decimal CalculateEstimatedPrice(DateOnly checkInDate, DateOnly checkOutDate, decimal baseNightlyPrice)
    {
        var nights = checkOutDate.DayNumber - checkInDate.DayNumber;
        return nights * baseNightlyPrice;
    }
}
