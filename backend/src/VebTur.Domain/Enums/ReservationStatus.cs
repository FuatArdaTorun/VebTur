namespace VebTur.Domain.Enums;

/// <summary>
/// Explicit ordinals matter here — this maps to a plain <c>integer</c> column in Postgres with no
/// value converter, so existing rows must keep resolving to the same status after any rename.
/// There used to be a <c>Pending = 0</c> value, but it was never actually persisted (both
/// creation and customer-edit set it in memory only to immediately overwrite it with
/// AwaitingApproval before the row is ever saved) — removed rather than kept as dead state.
/// </summary>
public enum ReservationStatus
{
    AwaitingApproval = 1,
    Confirmed = 2,
    Rejected = 3,
    Cancelled = 4,
}
