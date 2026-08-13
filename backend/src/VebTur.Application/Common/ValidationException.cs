namespace VebTur.Application.Common;

/// <summary>
/// Business-rule validation failure raised from an application service (e.g. slug already
/// in use, referenced amenity slug doesn't exist) — the kind of check that needs a database
/// round-trip and so can't live in a FluentValidation rule. Mapped to HTTP 400 at the API boundary.
/// </summary>
public class ValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }
}
