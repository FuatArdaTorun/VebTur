using VebTur.Application.Contracts.Admin;

namespace VebTur.Application.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
