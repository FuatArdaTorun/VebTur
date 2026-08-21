using VebTur.Application.Hotels;

namespace VebTur.Application.Admin;

/// <summary><c>IsActive: null</c> shows both active and soft-deleted hotels — needed so admin can find and reactivate one.</summary>
public record AdminHotelListRequest(string? Search, bool? IsActive, AdminHotelSortOrder Sort, int Page, int PageSize);
