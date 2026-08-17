using VebTur.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Slug).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Description).IsRequired();
        builder.Property(h => h.City).HasMaxLength(100).IsRequired();
        builder.Property(h => h.Country).HasMaxLength(100).IsRequired();
        builder.Property(h => h.Address).HasMaxLength(300).IsRequired();
        builder.Property(h => h.OfficialWebsiteUrl).HasMaxLength(500);
        builder.Property(h => h.GooglePlaceId).HasMaxLength(200);
        builder.Property(h => h.GoogleRating).HasPrecision(2, 1);

        builder.HasIndex(h => h.Slug).IsUnique();
        builder.HasIndex(h => h.City);
        builder.HasIndex(h => h.StarRating);

        builder.HasMany(h => h.Images)
            .WithOne(i => i.Hotel)
            .HasForeignKey(i => i.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.RoomTypes)
            .WithOne(r => r.Hotel)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.Supervisors)
            .WithOne(s => s.Hotel)
            .HasForeignKey(s => s.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.ExternalProfiles)
            .WithOne(p => p.Hotel)
            .HasForeignKey(p => p.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.ExternalRatings)
            .WithOne(r => r.Hotel)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
