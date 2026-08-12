using VebTur.Domain.Enums;

namespace VebTur.Domain.Entities;

public class ExternalHotelProfile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public ExternalProvider Provider { get; set; }
    public required string ExternalId { get; set; }
    public string? ExternalUrl { get; set; }
}
