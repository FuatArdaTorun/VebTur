using VebTur.Domain.Entities;
using VebTur.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(f => f.Id);

        // One favorite per (user, hotel) pair — AddFavoriteAsync is idempotent on top of this.
        builder.HasIndex(f => new { f.UserId, f.HotelId }).IsUnique();

        // Cascade, unlike Review.HotelId's Restrict — a favorite has no historical value worth
        // preserving, so a hotel with only favorites (no reservations) stays deletable, and its
        // favorites just disappear rather than blocking the delete.
        builder.HasOne(f => f.Hotel)
            .WithMany()
            .HasForeignKey(f => f.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cascade, unlike Review.UserId's SetNull — a favorite has no content independent of the
        // user (no guest-name snapshot to fall back on), so it should simply vanish with the account.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
