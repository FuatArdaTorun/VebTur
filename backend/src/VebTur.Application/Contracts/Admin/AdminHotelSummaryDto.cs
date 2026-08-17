namespace VebTur.Application.Contracts.Admin;

/// <summary>GET-list row — enough to identify and triage a hotel from the admin list screen.</summary>
public record AdminHotelSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string City,
    bool IsActive,
    string? ThumbnailUrl,
    DateTime UpdatedAtUtc,
    /// <summary>True if any ReservationRequest still references this hotel — it can't be permanently deleted (FK Restrict) until that's resolved. Lets the list hide the bulk-delete checkbox for it up front instead of the admin discovering the block only after selecting it.</summary>
    bool HasReservationHistory);
