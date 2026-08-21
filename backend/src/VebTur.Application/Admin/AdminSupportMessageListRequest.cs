using VebTur.Application.Support;

namespace VebTur.Application.Admin;

public record AdminSupportMessageListRequest(string? Search, AdminSupportMessageSortOrder Sort, int Page, int PageSize);
