using VebTur.Application.Common;
using VebTur.Application.Contracts.Reviews;
using VebTur.Application.Reviews;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Reviews;

public class ReviewService(VebTurDbContext db) : IReviewService
{
    public async Task<HotelReviewsResponseDto> GetHotelReviewsAsync(Guid hotelId, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var reviews = await db.Reviews.AsNoTracking()
            .Where(r => r.HotelId == hotelId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReviewDto(
                r.Id,
                r.Rating,
                r.Comment,
                r.ReservationRequest!.GuestFullName,
                r.ReservationRequest.RoomType!.Name,
                r.ReservationRequest.CheckInDate,
                r.ReservationRequest.CheckOutDate,
                r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var reviewable = currentUserId is null
            ? []
            : await db.ReservationRequests.AsNoTracking()
                .Where(res => res.HotelId == hotelId
                    && res.UserId == currentUserId
                    && res.Status == ReservationStatus.Confirmed
                    && !db.Reviews.Any(rv => rv.ReservationRequestId == res.Id))
                .OrderByDescending(res => res.CheckInDate)
                .Select(res => new ReviewableReservationDto(res.Id, res.RoomType!.Name, res.CheckInDate, res.CheckOutDate))
                .ToListAsync(cancellationToken);

        return new HotelReviewsResponseDto(reviews, reviewable);
    }

    public async Task<ReviewDto> CreateAsync(Guid hotelId, Guid userId, CreateReviewDto dto, CancellationToken cancellationToken)
    {
        var reservation = await db.ReservationRequests
            .FirstOrDefaultAsync(r => r.Id == dto.ReservationRequestId, cancellationToken);

        if (reservation is null || reservation.HotelId != hotelId || reservation.UserId != userId)
        {
            throw new ValidationException(nameof(dto.ReservationRequestId), "That reservation doesn't belong to you at this hotel.");
        }

        if (reservation.Status != ReservationStatus.Confirmed)
        {
            throw new ValidationException(nameof(dto.ReservationRequestId), "Only a confirmed reservation can be reviewed.");
        }

        var alreadyReviewed = await db.Reviews.AnyAsync(r => r.ReservationRequestId == reservation.Id, cancellationToken);
        if (alreadyReviewed)
        {
            throw new ValidationException(nameof(dto.ReservationRequestId), "This stay already has a review.");
        }

        var review = new Review
        {
            HotelId = hotelId,
            ReservationRequestId = reservation.Id,
            UserId = userId,
            Rating = dto.Rating,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);

        var roomType = await db.RoomTypes.AsNoTracking()
            .Where(r => r.Id == reservation.RoomTypeId)
            .Select(r => r.Name)
            .FirstAsync(cancellationToken);

        return new ReviewDto(review.Id, review.Rating, review.Comment, reservation.GuestFullName, roomType, reservation.CheckInDate, reservation.CheckOutDate, review.CreatedAtUtc);
    }
}
