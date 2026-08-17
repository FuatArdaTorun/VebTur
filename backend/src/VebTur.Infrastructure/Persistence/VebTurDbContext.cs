using VebTur.Domain.Common;
using VebTur.Domain.Entities;
using VebTur.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Persistence;

public class VebTurDbContext(DbContextOptions<VebTurDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<HotelImage> HotelImages => Set<HotelImage>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<HotelAmenity> HotelAmenities => Set<HotelAmenity>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<HotelSupervisor> HotelSupervisors => Set<HotelSupervisor>();
    public DbSet<ExternalHotelProfile> ExternalHotelProfiles => Set<ExternalHotelProfile>();
    public DbSet<ExternalRating> ExternalRatings => Set<ExternalRating>();
    public DbSet<ReservationRequest> ReservationRequests => Set<ReservationRequest>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VebTurDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IHasTimestamps>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
