namespace VebTur.Domain.Entities;

public class HotelAmenity
{
    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public Guid AmenityId { get; set; }
    public Amenity? Amenity { get; set; }
}
