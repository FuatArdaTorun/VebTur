using VebTur.Application.Contracts.Support;
using VebTur.Application.Support;
using VebTur.Domain.Entities;
using VebTur.Infrastructure.Persistence;

namespace VebTur.Infrastructure.Support;

public class SupportMessageService(VebTurDbContext db) : ISupportMessageService
{
    public async Task<Guid> CreateAsync(Guid? userId, CreateSupportMessageDto dto, CancellationToken cancellationToken)
    {
        var message = new SupportMessage
        {
            UserId = userId,
            SenderName = dto.Name.Trim(),
            SenderEmail = dto.Email.Trim(),
            Subject = dto.Subject.Trim(),
            Message = dto.Message.Trim(),
        };

        db.SupportMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}
