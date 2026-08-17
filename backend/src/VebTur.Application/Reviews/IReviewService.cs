using VebTur.Application.Contracts.Reviews;

namespace VebTur.Application.Reviews;

public interface IReviewService
{
    Task<HotelReviewsResponseDto> GetHotelReviewsAsync(Guid hotelId, Guid? currentUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Throws <see cref="VebTur.Application.Common.ValidationException"/> (mapped to 400) if the
    /// reservation isn't the caller's own, isn't <c>Confirmed</c>, or already has a review.
    /// </summary>
    Task<ReviewDto> CreateAsync(Guid hotelId, Guid userId, CreateReviewDto dto, CancellationToken cancellationToken);
}
