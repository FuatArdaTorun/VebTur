using VebTur.Application.Admin;
using VebTur.Application.Contracts.Admin;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Admin;

public class AdminDashboardService(VebTurDbContext db) : IAdminDashboardService
{
    public async Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var awaitingApprovalReservationsCount = await db.ReservationRequests
            .CountAsync(r => r.Status == ReservationStatus.AwaitingApproval, cancellationToken);
        var activeHotelsCount = await db.Hotels.CountAsync(h => h.IsActive, cancellationToken);
        var newSupportMessagesCount = await db.SupportMessages.CountAsync(s => s.RepliedAtUtc == null, cancellationToken);
        var totalReviewsCount = await db.Reviews.CountAsync(cancellationToken);

        return new AdminDashboardSummaryDto(awaitingApprovalReservationsCount, activeHotelsCount, newSupportMessagesCount, totalReviewsCount);
    }
}
