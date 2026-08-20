using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;

namespace VebTur.Application.Admin;

public interface IAdminSupportMessageService
{
    Task<PagedResult<AdminSupportMessageDto>> GetMessagesAsync(AdminSupportMessageListRequest request, CancellationToken cancellationToken);

    Task<AdminSupportMessageDto?> GetMessageAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Overwrites any previous reply — a support message has one current answer, not a threaded conversation.</summary>
    Task<bool> ReplyAsync(Guid id, ReplySupportMessageDto dto, CancellationToken cancellationToken);

    /// <summary>Irreversible.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bulk variant of <see cref="DeleteAsync"/>. Ids that don't exist are silently ignored. Returns how many were actually removed.</summary>
    Task<int> DeleteManyAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
}
