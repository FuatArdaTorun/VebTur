using VebTur.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Recipient).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(300).IsRequired();
        builder.Property(n => n.ErrorMessage).HasMaxLength(1000);

        builder.HasIndex(n => n.ReservationRequestId);

        // Cascade — unlike ReservationRequest's own FKs, these logs are owned by their parent
        // request and have no independent meaning without it.
        builder.HasOne(n => n.ReservationRequest)
            .WithMany(r => r.NotificationLogs)
            .HasForeignKey(n => n.ReservationRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
