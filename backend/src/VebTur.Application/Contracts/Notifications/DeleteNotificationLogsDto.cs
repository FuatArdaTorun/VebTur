namespace VebTur.Application.Contracts.Notifications;

public record DeleteNotificationLogsDto(IReadOnlyList<Guid> Ids);
