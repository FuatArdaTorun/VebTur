using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;

namespace VebTur.Application.Admin;

public interface IAdminReviewService
{
    Task<PagedResult<AdminReviewSummaryDto>> GetReviewsAsync(AdminReviewListRequest request, CancellationToken cancellationToken);

    /// <summary>Soft — excludes the review from the public hotel page and its average rating; reversible via <see cref="UnhideAsync"/>.</summary>
    Task<bool> HideAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> UnhideAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Irreversible.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk variant of <see cref="DeleteAsync"/>. Ids that don't exist are silently ignored. Returns how many were actually removed.</summary>
    Task<int> DeleteManyAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
