namespace VebTur.Domain.Entities;

public class HotelImage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public required string Url { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
}
