namespace VebTur.Application.Contracts.Support;

/// <summary>Minimal confirmation returned after a successful submit — the sender doesn't have an inbox to view this in later, see SupportMessage.</summary>
public record SupportMessageDto(Guid Id, DateTime CreatedAtUtc);
