using VebTur.Application.Admin;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Notifications;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Admin;

public class AdminNotificationService(VebTurDbContext db) : IAdminNotificationService
{
    public async Task<PagedResult<AdminNotificationLogDto>> GetNotificationsAsync(AdminNotificationListRequest request, CancellationToken cancellationToken)
    {
        var query = db.NotificationLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.Recipient, pattern) ||
                EF.Functions.ILike(n.Subject, pattern) ||
                EF.Functions.ILike(n.ReservationRequest!.ReferenceNumber, pattern) ||
                EF.Functions.ILike(n.ReservationRequest.Hotel!.Name, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new AdminNotificationLogDto(
                n.Id, n.ReservationRequestId, n.ReservationRequest!.ReferenceNumber, n.ReservationRequest.Hotel!.Name,
                n.Type.ToString(), n.Recipient, n.Subject, n.CreatedAtUtc, n.SentAtUtc, n.ErrorMessage))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminNotificationLogDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<int> DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var matches = await db.NotificationLogs.Where(n => ids.Contains(n.Id)).ToListAsync(cancellationToken);
        db.NotificationLogs.RemoveRange(matches);
        await db.SaveChangesAsync(cancellationToken);
        return matches.Count;
    }
}
