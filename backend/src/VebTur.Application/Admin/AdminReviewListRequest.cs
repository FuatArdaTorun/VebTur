using VebTur.Application.Reviews;

namespace VebTur.Application.Admin;

public record AdminReviewListRequest(string? Search, AdminReviewSortOrder Sort, int Page, int PageSize);
