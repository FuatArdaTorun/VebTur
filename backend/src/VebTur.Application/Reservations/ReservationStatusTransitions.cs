using VebTur.Application.Common;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;

namespace VebTur.Application.Reservations;

/// <summary>
/// Pure state-transition helpers shared between customer self-service (ReservationRequestService)
/// and admin reservation management (AdminReservationService), so the availability-adjustment
/// rules live in exactly one place rather than being duplicated across the two callers.
/// Callers must have <c>RoomType</c> loaded (e.g. via <c>Include</c>) before calling <see cref="Cancel"/>.
/// </summary>
public static class ReservationStatusTransitions
{
    public static void Cancel(ReservationRequest reservation)
    {
        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Rejected)
        {
            throw new ValidationException(nameof(reservation.Status), "This reservation is already cancelled or rejected.");
        }

        if (reservation.Status == ReservationStatus.Confirmed)
        {
            reservation.RoomType!.AvailableCount += 1;
        }

        reservation.Status = ReservationStatus.Cancelled;
    }
}
