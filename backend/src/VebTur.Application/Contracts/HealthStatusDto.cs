namespace VebTur.Application.Contracts;

public record HealthStatusDto(string Status, bool DatabaseReachable, DateTimeOffset TimestampUtc);
