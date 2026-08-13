namespace VebTur.Domain.Common;

public interface IHasTimestamps
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
}
