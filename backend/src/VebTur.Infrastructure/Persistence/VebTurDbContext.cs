using VebTur.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Persistence;

public class VebTurDbContext(DbContextOptions<VebTurDbContext> options) : DbContext(options)
{
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<HotelImage> HotelImages => Set<HotelImage>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<HotelAmenity> HotelAmenities => Set<HotelAmenity>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<HotelSupervisor> HotelSupervisors => Set<HotelSupervisor>();
    public DbSet<ExternalHotelProfile> ExternalHotelProfiles => Set<ExternalHotelProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VebTurDbContext).Assembly);
    }
}
