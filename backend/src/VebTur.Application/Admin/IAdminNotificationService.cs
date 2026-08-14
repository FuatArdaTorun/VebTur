using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Notifications;

namespace VebTur.Application.Admin;

public interface IAdminNotificationService
{
    Task<PagedResult<AdminNotificationLogDto>> GetNotificationsAsync(AdminNotificationListRequest request, CancellationToken cancellationToken);
}
