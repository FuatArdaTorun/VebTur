using VebTur.Domain.Entities;
using VebTur.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class ReservationRequestConfiguration : IEntityTypeConfiguration<ReservationRequest>
{
    public void Configure(EntityTypeBuilder<ReservationRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReferenceNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.GuestFullName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.GuestEmail).HasMaxLength(320).IsRequired();
        builder.Property(r => r.GuestPhone).HasMaxLength(30).IsRequired();
        builder.Property(r => r.SpecialRequests).HasMaxLength(1000);
        builder.Property(r => r.Currency).HasMaxLength(3).IsRequired();
        builder.Property(r => r.EstimatedPrice).HasPrecision(10, 2);

        builder.HasIndex(r => r.ReferenceNumber).IsUnique();
        builder.HasIndex(r => r.HotelId);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.Status);

        // Restrict (not Cascade) — a hotel/room type edit or the admin's permanent-delete
        // feature must never silently orphan or wipe out historical reservation requests.
        builder.HasOne(r => r.Hotel)
            .WithMany()
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RoomType)
            .WithMany()
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configured here (Infrastructure), not on the entity itself — ApplicationUser lives in
        // VebTur.Infrastructure.Auth, which Domain can never reference. SetNull so an account
        // deletion (not currently possible from the UI, but not precluded) never blocks on
        // reservation history the way Restrict would.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
