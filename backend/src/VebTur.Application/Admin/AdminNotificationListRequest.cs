using VebTur.Domain.Enums;

namespace VebTur.Application.Admin;

public record AdminNotificationListRequest(NotificationStatus? Status, string? Search, int Page, int PageSize);
