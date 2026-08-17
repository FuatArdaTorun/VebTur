using VebTur.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class ExternalRatingConfiguration : IEntityTypeConfiguration<ExternalRating>
{
    public void Configure(EntityTypeBuilder<ExternalRating> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating).HasPrecision(3, 2);
        builder.Property(r => r.MaximumRating).HasPrecision(3, 2);

        builder.HasIndex(r => new { r.HotelId, r.Provider }).IsUnique();
    }
}
