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

    /// <summary>
    /// Permanently removes the reservation request (and its notification logs) from the database.
    /// Throws <see cref="VebTur.Application.Common.ValidationException"/> if it's still AwaitingApproval —
    /// it must be confirmed or rejected first.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Bulk variant of <see cref="DeleteAsync"/>. Unlike the single-item version, AwaitingApproval ids
    /// are silently skipped rather than rejecting the whole batch — the admin UI never lets one be
    /// selected in the first place, so this only matters as a defense-in-depth guard against a
    /// hand-crafted request. Ids that don't exist are likewise silently ignored. Returns how many
    /// were actually removed.
    /// </summary>
    Task<int> DeleteManyAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
