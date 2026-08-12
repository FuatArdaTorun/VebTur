using VebTur.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class ExternalHotelProfileConfiguration : IEntityTypeConfiguration<ExternalHotelProfile>
{
    public void Configure(EntityTypeBuilder<ExternalHotelProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ExternalUrl).HasMaxLength(500);

        builder.HasIndex(p => new { p.HotelId, p.Provider }).IsUnique();
    }
}
