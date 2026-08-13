namespace VebTur.Application.Contracts.Admin;

/// <summary>GET-list row — enough to identify and triage a hotel from the admin list screen.</summary>
public record AdminHotelSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string City,
    bool IsActive,
    string? ThumbnailUrl,
    DateTime UpdatedAtUtc);
