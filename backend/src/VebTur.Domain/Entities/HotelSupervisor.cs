namespace VebTur.Domain.Entities;

public class HotelSupervisor
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    public required string FullName { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
}
