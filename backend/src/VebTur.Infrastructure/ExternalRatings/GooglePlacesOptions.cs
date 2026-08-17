namespace VebTur.Infrastructure.ExternalRatings;

public class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    /// <summary>Unset in dev/CI by default — <see cref="GooglePlacesRatingProvider"/> falls back to demo data when null/empty, never blocking the app.</summary>
    public string? ApiKey { get; set; }
}
