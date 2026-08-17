namespace VebTur.Application.Contracts.Reviews;

public record CreateReviewDto(Guid ReservationRequestId, int Rating, string? Comment);
