using VebTur.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class HotelSupervisorConfiguration : IEntityTypeConfiguration<HotelSupervisor>
{
    public void Configure(EntityTypeBuilder<HotelSupervisor> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FullName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(320).IsRequired();

        builder.HasIndex(s => s.HotelId);
    }
}
