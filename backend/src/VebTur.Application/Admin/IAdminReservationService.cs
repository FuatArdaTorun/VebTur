using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Reservations;

namespace VebTur.Application.Admin;

public interface IAdminReservationService
{
    Task<PagedResult<AdminReservationSummaryDto>> GetReservationsAsync(AdminReservationListRequest request, CancellationToken cancellationToken);

    Task<AdminReservationDetailDto?> GetReservationByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Only from Pending/Sent. Throws <see cref="VebTur.Application.Common.ValidationException"/> when the room type has no availability left.</summary>
    Task<bool> ConfirmAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Only from Pending/Sent — a reservation that was never confirmed never touched availability.</summary>
    Task<bool> RejectAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Only from Pending/Sent/Confirmed. Releases availability back if it had been Confirmed.</summary>
    Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Permanently removes the reservation request (and its notification logs) from the database.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
