using VebTur.Application.Contracts.Support;

namespace VebTur.Application.Support;

public interface ISupportMessageService
{
    Task<Guid> CreateAsync(Guid? userId, CreateSupportMessageDto dto, CancellationToken cancellationToken);
}
