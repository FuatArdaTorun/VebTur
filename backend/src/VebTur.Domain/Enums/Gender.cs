namespace VebTur.Domain.Enums;

/// <summary>
/// Explicit ordinals matter here — this maps to a plain <c>integer</c> column in Postgres with no
/// value converter, so existing rows must keep resolving to the same value after any rename.
/// Optional on <c>ApplicationUser</c> — a customer is never required to disclose this.
/// </summary>
public enum Gender
{
    Female = 1,
    Male = 2,
    Other = 3,
    PreferNotToSay = 4,
}
