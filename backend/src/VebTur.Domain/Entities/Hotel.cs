namespace VebTur.Domain.Entities;

public class Hotel
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Description { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string Address { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int StarRating { get; set; }

    public string? OfficialWebsiteUrl { get; set; }
    public string? GooglePlaceId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<HotelImage> Images { get; set; } = [];
    public List<RoomType> RoomTypes { get; set; } = [];
    public List<HotelSupervisor> Supervisors { get; set; } = [];
    public List<HotelAmenity> HotelAmenities { get; set; } = [];
    public List<ExternalHotelProfile> ExternalProfiles { get; set; } = [];
}
