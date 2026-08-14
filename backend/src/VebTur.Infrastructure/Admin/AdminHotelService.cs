using VebTur.Application.Admin;
using VebTur.Application.Common;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Domain.Entities;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Admin;

public class AdminHotelService(VebTurDbContext db) : IAdminHotelService
{
    public async Task<PagedResult<AdminHotelSummaryDto>> GetHotelsAsync(AdminHotelListRequest request, CancellationToken cancellationToken)
    {
        var query = db.Hotels.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(h => EF.Functions.ILike(h.Name, pattern) || EF.Functions.ILike(h.City, pattern));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(h => h.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(h => h.UpdatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(h => new AdminHotelSummaryDto(
                h.Id,
                h.Name,
                h.Slug,
                h.City,
                h.IsActive,
                h.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
                h.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminHotelSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<AdminHotelDetailDto?> GetHotelByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var hotel = await LoadForReadAsync(id, cancellationToken);
        return hotel is null ? null : ToDetailDto(hotel);
    }

    public async Task<AdminHotelDetailDto> CreateHotelAsync(AdminHotelUpsertDto dto, CancellationToken cancellationToken)
    {
        await EnsureSlugAvailableAsync(dto.Slug, excludingId: null, cancellationToken);
        var amenityIds = await ResolveAmenityIdsAsync(dto.AmenitySlugs, cancellationToken);

        var hotel = new Hotel
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            City = dto.City,
            Country = dto.Country,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            StarRating = dto.StarRating,
            OfficialWebsiteUrl = dto.OfficialWebsiteUrl,
            GooglePlaceId = dto.GooglePlaceId,
            PhoneNumber = dto.PhoneNumber,
            GoogleRating = dto.GoogleRating,
            GoogleRatingCount = dto.GoogleRatingCount,
            GoogleRatingCapturedAtUtc = dto.GoogleRating.HasValue ? DateTime.UtcNow : null,
            IsActive = dto.IsActive,
        };

        hotel.Images = dto.Images
            .Select(i => new HotelImage { HotelId = hotel.Id, Url = i.Url, AltText = i.AltText, DisplayOrder = i.DisplayOrder })
            .ToList();
        hotel.RoomTypes = dto.RoomTypes
            .Select(r => new RoomType { HotelId = hotel.Id, Name = r.Name, Description = r.Description, Capacity = r.Capacity, BaseNightlyPrice = r.BaseNightlyPrice, Currency = r.Currency, IsActive = r.IsActive })
            .ToList();
        hotel.Supervisors = dto.Supervisors
            .Select(s => new HotelSupervisor { HotelId = hotel.Id, FullName = s.FullName, Email = s.Email, IsActive = s.IsActive })
            .ToList();
        hotel.HotelAmenities = amenityIds
            .Select(amenityId => new HotelAmenity { HotelId = hotel.Id, AmenityId = amenityId })
            .ToList();

        db.Hotels.Add(hotel);
        await db.SaveChangesAsync(cancellationToken);

        return (await GetHotelByIdAsync(hotel.Id, cancellationToken))!;
    }

    public async Task<AdminHotelDetailDto?> UpdateHotelAsync(Guid id, AdminHotelUpsertDto dto, CancellationToken cancellationToken)
    {
        var hotel = await db.Hotels
            .Include(h => h.Images)
            .Include(h => h.RoomTypes)
            .Include(h => h.Supervisors)
            .Include(h => h.HotelAmenities)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

        if (hotel is null)
        {
            return null;
        }

        await EnsureSlugAvailableAsync(dto.Slug, excludingId: id, cancellationToken);
        var amenityIds = await ResolveAmenityIdsAsync(dto.AmenitySlugs, cancellationToken);

        hotel.Name = dto.Name;
        hotel.Slug = dto.Slug;
        hotel.Description = dto.Description;
        hotel.City = dto.City;
        hotel.Country = dto.Country;
        hotel.Address = dto.Address;
        hotel.Latitude = dto.Latitude;
        hotel.Longitude = dto.Longitude;
        hotel.StarRating = dto.StarRating;
        hotel.OfficialWebsiteUrl = dto.OfficialWebsiteUrl;
        hotel.GooglePlaceId = dto.GooglePlaceId;
        hotel.PhoneNumber = dto.PhoneNumber;
        if (hotel.GoogleRating != dto.GoogleRating)
        {
            hotel.GoogleRatingCapturedAtUtc = dto.GoogleRating.HasValue ? DateTime.UtcNow : null;
        }

        hotel.GoogleRating = dto.GoogleRating;
        hotel.GoogleRatingCount = dto.GoogleRatingCount;
        hotel.IsActive = dto.IsActive;

        // createFromDto calls db.Add(...) explicitly, not just existing.Add(...), because every
        // entity here has a client-generated Guid Id (Guid.CreateVersion7()) set at construction
        // time. When a new object is only attached via a navigation collection on an
        // already-tracked parent, EF Core's DetectChanges uses a "does the key look default?"
        // heuristic to decide Added vs. Modified — a non-default key makes it guess Modified,
        // which then fails as a 0-rows-affected DbUpdateConcurrencyException (UPDATE instead of
        // INSERT). An explicit db.Add(...) always wins over that heuristic.
        HotelAggregateReconciler.Reconcile(
            hotel.Images,
            dto.Images,
            getEntityId: e => e.Id,
            getDtoId: d => d.Id,
            applyToExisting: (e, d) => { e.Url = d.Url; e.AltText = d.AltText; e.DisplayOrder = d.DisplayOrder; },
            createFromDto: d =>
            {
                var image = new HotelImage { HotelId = hotel.Id, Url = d.Url, AltText = d.AltText, DisplayOrder = d.DisplayOrder };
                db.Add(image);
                return image;
            });

        HotelAggregateReconciler.Reconcile(
            hotel.RoomTypes,
            dto.RoomTypes,
            getEntityId: e => e.Id,
            getDtoId: d => d.Id,
            applyToExisting: (e, d) => { e.Name = d.Name; e.Description = d.Description; e.Capacity = d.Capacity; e.BaseNightlyPrice = d.BaseNightlyPrice; e.Currency = d.Currency; e.IsActive = d.IsActive; },
            createFromDto: d =>
            {
                var room = new RoomType { HotelId = hotel.Id, Name = d.Name, Description = d.Description, Capacity = d.Capacity, BaseNightlyPrice = d.BaseNightlyPrice, Currency = d.Currency, IsActive = d.IsActive };
                db.Add(room);
                return room;
            });

        HotelAggregateReconciler.Reconcile(
            hotel.Supervisors,
            dto.Supervisors,
            getEntityId: e => e.Id,
            getDtoId: d => d.Id,
            applyToExisting: (e, d) => { e.FullName = d.FullName; e.Email = d.Email; e.IsActive = d.IsActive; },
            createFromDto: d =>
            {
                var supervisor = new HotelSupervisor { HotelId = hotel.Id, FullName = d.FullName, Email = d.Email, IsActive = d.IsActive };
                db.Add(supervisor);
                return supervisor;
            });

        // Amenities are a pure join (composite PK, no surrogate Id) — diff by AmenityId instead
        // of clearing and re-adding everything. A blind Clear()-then-re-add would track a
        // Deleted and an Added row with the identical (HotelId, AmenityId) key for any amenity
        // that survives unchanged, which EF Core resolves incorrectly (DbUpdateConcurrencyException).
        var newAmenityIds = amenityIds.ToHashSet();
        var currentAmenityIds = hotel.HotelAmenities.Select(ha => ha.AmenityId).ToHashSet();

        foreach (var toRemove in hotel.HotelAmenities.Where(ha => !newAmenityIds.Contains(ha.AmenityId)).ToList())
        {
            hotel.HotelAmenities.Remove(toRemove);
        }

        foreach (var amenityId in newAmenityIds.Where(a => !currentAmenityIds.Contains(a)))
        {
            db.Add(new HotelAmenity { HotelId = hotel.Id, AmenityId = amenityId });
        }

        await db.SaveChangesAsync(cancellationToken);

        return await GetHotelByIdAsync(hotel.Id, cancellationToken);
    }

    public async Task<bool> DeactivateHotelAsync(Guid id, CancellationToken cancellationToken)
        => await SetActiveAsync(id, isActive: false, cancellationToken);

    public async Task<bool> ReactivateHotelAsync(Guid id, CancellationToken cancellationToken)
        => await SetActiveAsync(id, isActive: true, cancellationToken);

    public async Task<bool> DeleteHotelPermanentlyAsync(Guid id, CancellationToken cancellationToken)
    {
        var hotel = await db.Hotels.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (hotel is null)
        {
            return false;
        }

        db.Hotels.Remove(hotel);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var hotel = await db.Hotels.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (hotel is null)
        {
            return false;
        }

        hotel.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<Hotel?> LoadForReadAsync(Guid id, CancellationToken cancellationToken) => db.Hotels
        .AsNoTracking()
        .Include(h => h.Images)
        .Include(h => h.RoomTypes)
        .Include(h => h.Supervisors)
        .Include(h => h.HotelAmenities).ThenInclude(ha => ha.Amenity)
        .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    private async Task EnsureSlugAvailableAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = db.Hotels.Where(h => h.Slug == slug);
        if (excludingId.HasValue)
        {
            query = query.Where(h => h.Id != excludingId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new ValidationException(nameof(AdminHotelUpsertDto.Slug), $"Slug '{slug}' is already in use by another hotel.");
        }
    }

    private async Task<List<Guid>> ResolveAmenityIdsAsync(IReadOnlyList<string> slugs, CancellationToken cancellationToken)
    {
        if (slugs.Count == 0)
        {
            return [];
        }

        var amenities = await db.Amenities.Where(a => slugs.Contains(a.Slug)).ToListAsync(cancellationToken);
        var missing = slugs.Except(amenities.Select(a => a.Slug)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationException(nameof(AdminHotelUpsertDto.AmenitySlugs), $"Unknown amenity slug(s): {string.Join(", ", missing)}");
        }

        return amenities.Select(a => a.Id).ToList();
    }

    private static AdminHotelDetailDto ToDetailDto(Hotel h) => new(
        h.Id, h.Name, h.Slug, h.Description, h.City, h.Country, h.Address, h.Latitude, h.Longitude,
        h.StarRating, h.OfficialWebsiteUrl, h.GooglePlaceId, h.PhoneNumber, h.GoogleRating, h.GoogleRatingCount,
        h.IsActive, h.CreatedAtUtc, h.UpdatedAtUtc,
        h.Images.OrderBy(i => i.DisplayOrder).Select(i => new AdminHotelImageDto(i.Id, i.Url, i.AltText, i.DisplayOrder)).ToList(),
        h.RoomTypes.Select(r => new AdminRoomTypeDto(r.Id, r.Name, r.Description, r.Capacity, r.BaseNightlyPrice, r.Currency, r.IsActive)).ToList(),
        h.Supervisors.Select(s => new AdminHotelSupervisorDto(s.Id, s.FullName, s.Email, s.IsActive)).ToList(),
        h.HotelAmenities.Select(ha => ha.Amenity!.Slug).ToList());
}
