using VebTur.Application.Admin;
using VebTur.Application.Contracts;
using VebTur.Application.Contracts.Admin;
using VebTur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Admin;

public class AdminSupportMessageService(VebTurDbContext db) : IAdminSupportMessageService
{
    public async Task<PagedResult<AdminSupportMessageDto>> GetMessagesAsync(AdminSupportMessageListRequest request, CancellationToken cancellationToken)
    {
        var query = db.SupportMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.SenderName, pattern) ||
                EF.Functions.ILike(m.SenderEmail, pattern) ||
                EF.Functions.ILike(m.Subject, pattern) ||
                EF.Functions.ILike(m.Message, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new AdminSupportMessageDto(m.Id, m.SenderName, m.SenderEmail, m.Subject, m.Message, m.ReplyMessage, m.RepliedAtUtc, m.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminSupportMessageDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<AdminSupportMessageDto?> GetMessageAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.SupportMessages.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new AdminSupportMessageDto(m.Id, m.SenderName, m.SenderEmail, m.Subject, m.Message, m.ReplyMessage, m.RepliedAtUtc, m.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ReplyAsync(Guid id, ReplySupportMessageDto dto, CancellationToken cancellationToken)
    {
        var message = await db.SupportMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return false;
        }

        message.ReplyMessage = dto.ReplyMessage.Trim();
        message.RepliedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var message = await db.SupportMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return false;
        }

        db.SupportMessages.Remove(message);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> DeleteManyAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var messages = await db.SupportMessages.Where(m => ids.Contains(m.Id)).ToListAsync(cancellationToken);
        db.SupportMessages.RemoveRange(messages);
        await db.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }
}
