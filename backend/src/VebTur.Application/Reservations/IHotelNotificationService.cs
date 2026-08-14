using VebTur.Domain.Entities;

namespace VebTur.Application.Reservations;

/// <summary>
/// VebTur has no real hotel booking API to call — the
/// implementation of this interface is a demo/simulated notification, not a real integration.
/// Kept as an interface anyway so a real channel is a drop-in replacement later, not a redesign.
/// </summary>
public interface IHotelNotificationService
{
    Task NotifyHotelAsync(ReservationRequest reservation, CancellationToken cancellationToken);
}
