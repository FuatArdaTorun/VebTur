namespace VebTur.Application.Contracts.Admin;

public record AdminDashboardSummaryDto(
    int AwaitingApprovalReservationsCount,
    int ActiveHotelsCount,
    int NewSupportMessagesCount,
    int TotalReviewsCount);
