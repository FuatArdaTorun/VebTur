using VebTur.Application.Reservations;
using VebTur.Domain.Entities;
using VebTur.Domain.Enums;
using VebTur.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Persistence.Seed;

/// <summary>
/// Dev-only demo content: a handful of synthetic customer accounts, each with a Confirmed
/// reservation at a real seeded hotel, each reservation carrying one review — so every hotel has
/// real-looking content to show before real customers add their own.
/// Reviewer names/text are VebTur's own demo content, not claimed or scraped from any
/// real Google/OTA review.
/// The hotel data itself is left untouched. Nobody is meant to log in as these accounts, so
/// their passwords are random and not recorded anywhere.
/// </summary>
public static class ReviewSeeder
{
    private static readonly (string Name, string EmailLocalPart)[] DemoReviewers =
    [
        ("Elif Yılmaz", "elif.demo"),
        ("Mehmet Kaya", "mehmet.demo"),
        ("Ayşe Demir", "ayse.demo"),
        ("Can Öztürk", "can.demo"),
    ];

    // Weighted toward positive (mostly 4-5★, an occasional 3★) — the mix a genuinely well-run
    // hotel would realistically get, not a wall of identical 5-star praise. A null comment is a
    // valid review on its own (rating with no written text).
    private static readonly (int Rating, string? Comment)[] CommentPool =
    [
        (5, "Great location and very clean rooms. The staff was incredibly helpful throughout our stay."),
        (5, "Exceptional service from start to finish. Highly recommend the sea-view rooms."),
        (5, "One of the best hotel experiences we've had in Antalya. Everything was spotless."),
        (5, "Would definitely come back — perfect for a family trip, kids loved the pool."),
        (5, null),
        (5, "Outstanding breakfast spread and a genuinely warm welcome at check-in."),
        (5, "Beautiful grounds, quiet at night, and the pool area was immaculate."),
        (4, "Breakfast was excellent, pool area could use a refresh, but overall a solid stay."),
        (4, "Good value overall, a bit noisy near the bar in the evenings."),
        (4, "Spacious rooms and friendly staff. Would stay again."),
        (4, null),
        (4, "Comfortable stay, though the Wi-Fi was patchy in the room."),
        (4, "Great pool area and helpful reception, checkout could have been quicker."),
        (3, "Room was smaller than expected for the price, but check-in was smooth and the view was lovely."),
        (3, "Decent stay overall, though housekeeping missed our room one day."),
    ];

    public static async Task SeedAsync(VebTurDbContext db, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken = default)
    {
        var hotels = await db.Hotels.AsNoTracking()
            .Where(h => h.IsActive && h.RoomTypes.Any(r => r.IsActive))
            .Include(h => h.RoomTypes)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);

        if (hotels.Count == 0)
        {
            return;
        }

        var hotelIdsWithReviews = (await db.Reviews.AsNoTracking().Select(r => r.HotelId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        var hotelsNeedingReviews = hotels.Where(h => !hotelIdsWithReviews.Contains(h.Id)).ToList();
        if (hotelsNeedingReviews.Count == 0)
        {
            return;
        }

        var reviewers = new ApplicationUser[DemoReviewers.Length];
        for (var i = 0; i < DemoReviewers.Length; i++)
        {
            reviewers[i] = await EnsureDemoCustomerAsync(userManager, DemoReviewers[i].Name, DemoReviewers[i].EmailLocalPart);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reservations = new List<ReservationRequest>();
        var reviews = new List<Review>();
        var commentCursor = 0;
        var dayOffset = 12;

        for (var hotelIndex = 0; hotelIndex < hotelsNeedingReviews.Count; hotelIndex++)
        {
            var hotel = hotelsNeedingReviews[hotelIndex];
            var roomType = hotel.RoomTypes.First(r => r.IsActive);
            var reviewCount = hotelIndex % 2 == 0 ? 4 : 3; // alternate 4/3 per hotel

            for (var i = 0; i < reviewCount; i++)
            {
                var reviewer = reviewers[i % reviewers.Length];
                var (rating, comment) = CommentPool[commentCursor % CommentPool.Length];
                commentCursor++;

                var nights = 2 + (dayOffset % 5);
                var checkIn = today.AddDays(-dayOffset);
                var checkOut = checkIn.AddDays(nights);
                dayOffset += 11;

                var reservation = new ReservationRequest
                {
                    ReferenceNumber = ReferenceNumberGenerator.Generate(),
                    HotelId = hotel.Id,
                    RoomTypeId = roomType.Id,
                    UserId = reviewer.Id,
                    GuestFullName = reviewer.DisplayName,
                    GuestEmail = reviewer.Email!,
                    GuestPhone = "+90 500 000 00 00",
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    AdultCount = 2,
                    ChildCount = 0,
                    EstimatedPrice = ReservationPricingCalculator.CalculateEstimatedPrice(checkIn, checkOut, roomType.BaseNightlyPrice),
                    Currency = roomType.Currency,
                    Status = ReservationStatus.Confirmed,
                    NotificationSentAtUtc = DateTime.UtcNow,
                };
                reservations.Add(reservation);

                reviews.Add(new Review
                {
                    HotelId = hotel.Id,
                    ReservationRequestId = reservation.Id,
                    UserId = reviewer.Id,
                    Rating = rating,
                    Comment = comment,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-dayOffset + nights),
                });
            }
        }

        db.ReservationRequests.AddRange(reservations);
        db.Reviews.AddRange(reviews);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ApplicationUser> EnsureDemoCustomerAsync(UserManager<ApplicationUser> userManager, string displayName, string emailLocalPart)
    {
        var email = $"{emailLocalPart}@vebtur.local";
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
        };

        var password = $"Demo-{Guid.NewGuid():N}A1!";
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed demo reviewer '{email}': {errors}");
        }

        await userManager.AddToRoleAsync(user, IdentitySeeder.CustomerRoleName);
        return user;
    }
}
