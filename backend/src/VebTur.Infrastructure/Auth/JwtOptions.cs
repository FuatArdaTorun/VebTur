namespace VebTur.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string SigningKey { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int ExpiryHours { get; set; } = 8;
}
