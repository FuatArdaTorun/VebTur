using VebTur.Application.Admin;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Admin;

public class AdminReviewService(VebTurDbContext db) : IAdminReviewService
{
    public async Task<PagedResult<AdminReviewSummaryDto>> GetReviewsAsync(AdminReviewListRequest request, CancellationToken cancellationToken)
    {
        var query = db.Reviews.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.Hotel!.Name, pattern) ||
                EF.Functions.ILike(r.ReservationRequest!.GuestFullName, pattern) ||
                (r.Comment != null && EF.Functions.ILike(r.Comment, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminReviewSummaryDto(
                r.Id,
                r.HotelId,
                r.Hotel!.Name,
                r.ReservationRequest!.GuestFullName,
                r.Rating,
                r.Comment,
                r.ReservationRequest.RoomType!.Name,
                r.ReservationRequest.CheckInDate,
                r.ReservationRequest.CheckOutDate,
                r.IsHidden,
                r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminReviewSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<bool> HideAsync(Guid id, CancellationToken cancellationToken)
        => await SetHiddenAsync(id, isHidden: true, cancellationToken);

    public async Task<bool> UnhideAsync(Guid id, CancellationToken cancellationToken)
        => await SetHiddenAsync(id, isHidden: false, cancellationToken);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (review is null)
        {
            return false;
        }

        db.Reviews.Remove(review);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DeleteManyAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var reviews = await db.Reviews.Where(r => ids.Contains(r.Id)).ToListAsync(cancellationToken);
        db.Reviews.RemoveRange(reviews);
        await db.SaveChangesAsync(cancellationToken);
        return reviews.Count;
    }

    private async Task<bool> SetHiddenAsync(Guid id, bool isHidden, CancellationToken cancellationToken)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (review is null)
        {
            return false;
        }

        review.IsHidden = isHidden;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
