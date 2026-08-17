using VebTur.Domain.Entities;
using VebTur.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Comment).HasMaxLength(2000);

        builder.HasIndex(r => r.HotelId);
        builder.HasIndex(r => r.IsHidden);
        builder.HasIndex(r => r.ReservationRequestId).IsUnique();

        // Restrict, denormalized for query convenience — in practice ReservationRequest's own
        // Restrict FK to Hotel already blocks a hotel delete first whenever any reservation
        // (reviewed or not) exists, so this rarely fires as the actual blocking path.
        builder.HasOne(r => r.Hotel)
            .WithMany()
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade — unlike ReservationRequest's own FKs, a review has no independent meaning
        // without the stay it's about (same reasoning as NotificationLog's FK to ReservationRequest).
        builder.HasOne(r => r.ReservationRequest)
            .WithMany()
            .HasForeignKey(r => r.ReservationRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configured here (Infrastructure), not on the entity itself — ApplicationUser lives in
        // VebTur.Infrastructure.Auth, which Domain can never reference. SetNull so an account
        // deletion never destroys the review; the reviewer's display name is read from the
        // linked ReservationRequest.GuestFullName snapshot instead of stored on Review itself.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
