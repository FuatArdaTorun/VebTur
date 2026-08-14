using VebTur.Application.Common;
using VebTur.Application.Reservations;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;

namespace VebTur.UnitTests.Reservations;

public class ReservationStatusTransitionsTests
{
    private static ReservationRequest BuildReservation(ReservationStatus status, int availableCount) => new()
    {
        ReferenceNumber = "VEB-TEST0001",
        GuestFullName = "Jane Guest",
        GuestEmail = "jane@example.com",
        GuestPhone = "+90 555 000 00 00",
        Currency = "TRY",
        Status = status,
        RoomType = new RoomType { Name = "Standard", Description = "d", Currency = "TRY", AvailableCount = availableCount },
    };

    [Fact]
    public void Cancel_FromConfirmed_ReleasesOneAvailableSlot()
    {
        var reservation = BuildReservation(ReservationStatus.Confirmed, availableCount: 2);

        ReservationStatusTransitions.Cancel(reservation);

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(3, reservation.RoomType!.AvailableCount);
    }

    [Fact]
    public void Cancel_FromAwaitingApproval_DoesNotChangeAvailableCount()
    {
        var reservation = BuildReservation(ReservationStatus.AwaitingApproval, availableCount: 2);

        ReservationStatusTransitions.Cancel(reservation);

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(2, reservation.RoomType!.AvailableCount);
    }

    [Theory]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Rejected)]
    public void Cancel_FromTerminalStatus_ThrowsValidationException(ReservationStatus status)
    {
        var reservation = BuildReservation(status, availableCount: 2);

        Assert.Throws<ValidationException>(() => ReservationStatusTransitions.Cancel(reservation));
    }
}
