using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Notifications;

namespace VebTur.Application.Admin;

public interface IAdminNotificationService
{
    Task<PagedResult<AdminNotificationLogDto>> GetNotificationsAsync(AdminNotificationListRequest request, CancellationToken cancellationToken);

    /// <summary>Permanently deletes the given notification log rows. Ids that don't exist are silently ignored. Returns the number actually deleted.</summary>
    Task<int> DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
