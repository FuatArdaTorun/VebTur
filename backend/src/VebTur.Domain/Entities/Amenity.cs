namespace VebTur.Domain.Entities;

public class Amenity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? IconKey { get; set; }

    public List<HotelAmenity> HotelAmenities { get; set; } = [];
}
