using System.Security.Cryptography;

namespace VebTur.Application.Reservations;

/// <summary>
/// Pure candidate generator — no DB access, so it's unit-testable. The caller (which has a
/// DbContext) is responsible for checking uniqueness and retrying on the astronomically rare
/// collision (31^8 combinations).
/// </summary>
public static class ReferenceNumberGenerator
{
    // Excludes visually ambiguous characters (0/O, 1/I) since a guest may need to read this
    // back over the phone or retype it from a printed confirmation.
    private const string Charset = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int SuffixLength = 8;

    public static string Generate()
    {
        Span<char> suffix = stackalloc char[SuffixLength];
        for (var i = 0; i < SuffixLength; i++)
        {
            suffix[i] = Charset[RandomNumberGenerator.GetInt32(Charset.Length)];
        }

        return $"VEB-{new string(suffix)}";
    }
}
