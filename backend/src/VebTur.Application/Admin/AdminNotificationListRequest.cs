namespace VebTur.Application.Admin;

public record AdminNotificationListRequest(string? Search, AdminNotificationSortOrder Sort, int Page, int PageSize);
