using VebTur.Domain.Common;

namespace VebTur.Domain.Entities;

public class Hotel : IHasTimestamps
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
    /// <summary>
    /// Official star classification as stated by the hotel itself. Null when not
    /// confidently sourced from the hotel's own site — never a guessed value.
    /// </summary>
    public int? StarRating { get; set; }

    public string? OfficialWebsiteUrl { get; set; }
    public string? GooglePlaceId { get; set; }

    /// <summary>
    /// The hotel's own live-support/contact phone number, sourced from its official website
    /// (same sourcing policy as the rest of the hotel's data). Null when not confidently found.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The hotel's aggregate Google Maps rating (e.g. 4.5 out of 5), manually captured on
    /// <see cref="GoogleRatingCapturedAtUtc"/> since automated Google Places API integration
    /// is not connected. This is the public rating number only — never review text/content.
    /// Null when not confidently found. Distinct from <see cref="StarRating"/> (the hotel's
    /// own official classification), which is a different concept from a guest rating.
    /// </summary>
    public decimal? GoogleRating { get; set; }
    public int? GoogleRatingCount { get; set; }
    public DateTime? GoogleRatingCapturedAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<HotelImage> Images { get; set; } = [];
    public List<RoomType> RoomTypes { get; set; } = [];
    public List<HotelSupervisor> Supervisors { get; set; } = [];
    public List<HotelAmenity> HotelAmenities { get; set; } = [];
    public List<ExternalHotelProfile> ExternalProfiles { get; set; } = [];
    public List<ExternalRating> ExternalRatings { get; set; } = [];
}
