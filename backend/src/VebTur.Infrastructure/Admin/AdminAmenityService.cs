using VebTur.Application.Admin;
using VebTur.Application.Common;
using VebTur.Application.Contracts.Admin;
using VebTur.Domain.Entities;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Admin;

public class AdminAmenityService(VebTurDbContext db) : IAdminAmenityService
{
    public async Task<List<AdminAmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken)
    {
        return await db.Amenities.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AdminAmenityDto(a.Id, a.Name, a.Slug, a.IconKey, a.HotelAmenities.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminAmenityDto> CreateAmenityAsync(AdminAmenityUpsertDto dto, CancellationToken cancellationToken)
    {
        await EnsureSlugAvailableAsync(dto.Slug, excludingId: null, cancellationToken);

        var amenity = new Amenity { Name = dto.Name, Slug = dto.Slug, IconKey = dto.IconKey };
        db.Amenities.Add(amenity);
        await db.SaveChangesAsync(cancellationToken);

        return new AdminAmenityDto(amenity.Id, amenity.Name, amenity.Slug, amenity.IconKey, 0);
    }

    public async Task<AdminAmenityDto?> UpdateAmenityAsync(Guid id, AdminAmenityUpsertDto dto, CancellationToken cancellationToken)
    {
        var amenity = await db.Amenities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (amenity is null)
        {
            return null;
        }

        await EnsureSlugAvailableAsync(dto.Slug, excludingId: id, cancellationToken);

        amenity.Name = dto.Name;
        amenity.Slug = dto.Slug;
        amenity.IconKey = dto.IconKey;
        await db.SaveChangesAsync(cancellationToken);

        var hotelCount = await db.HotelAmenities.CountAsync(ha => ha.AmenityId == id, cancellationToken);
        return new AdminAmenityDto(amenity.Id, amenity.Name, amenity.Slug, amenity.IconKey, hotelCount);
    }

    public async Task<bool> DeleteAmenityAsync(Guid id, CancellationToken cancellationToken)
    {
        var amenity = await db.Amenities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (amenity is null)
        {
            return false;
        }

        db.Amenities.Remove(amenity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureSlugAvailableAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = db.Amenities.Where(a => a.Slug == slug);
        if (excludingId.HasValue)
        {
            query = query.Where(a => a.Id != excludingId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new ValidationException(nameof(AdminAmenityUpsertDto.Slug), $"Slug '{slug}' is already in use by another amenity.");
        }
    }
}
