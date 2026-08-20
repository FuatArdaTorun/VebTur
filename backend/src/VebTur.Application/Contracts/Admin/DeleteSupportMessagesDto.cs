namespace VebTur.Application.Contracts.Admin;

public record DeleteSupportMessagesDto(IReadOnlyList<Guid> Ids);
