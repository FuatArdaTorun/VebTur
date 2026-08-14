namespace VebTur.Domain.Enums;

/// <summary>
/// VebTur has no real hotel booking API to call, so
/// this only ever has one value today — a simulated "sent to the hotel's system" notification,
/// logged rather than actually dispatched anywhere. Kept as an enum, not a bool, so a real
/// channel (e.g. email) can be added later without a redesign.
/// </summary>
public enum NotificationType
{
    DemoHotelApi = 0,
}
