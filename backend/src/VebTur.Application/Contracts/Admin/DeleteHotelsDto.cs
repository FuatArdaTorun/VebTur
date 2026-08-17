namespace VebTur.Application.Contracts.Admin;

public record DeleteHotelsDto(IReadOnlyList<Guid> Ids);
