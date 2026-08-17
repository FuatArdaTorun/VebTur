namespace VebTur.Application.Contracts.Reviews;

/// <summary><see cref="MyReviewableReservations"/> is empty unless the caller is authenticated.</summary>
public record HotelReviewsResponseDto(
    IReadOnlyList<ReviewDto> Reviews,
    IReadOnlyList<ReviewableReservationDto> MyReviewableReservations);
