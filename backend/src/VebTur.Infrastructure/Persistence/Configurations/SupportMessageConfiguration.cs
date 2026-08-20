using VebTur.Domain.Entities;
using VebTur.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VebTur.Infrastructure.Persistence.Configurations;

public class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderName).HasMaxLength(200);
        builder.Property(m => m.SenderEmail).HasMaxLength(320);
        builder.Property(m => m.Subject).HasMaxLength(200);
        builder.Property(m => m.Message).HasMaxLength(4000);
        builder.Property(m => m.ReplyMessage).HasMaxLength(4000);

        // SetNull, same reasoning as Review.UserId — the message stands on its own (sender
        // name/email are captured directly on it), so an account deletion shouldn't destroy it.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
