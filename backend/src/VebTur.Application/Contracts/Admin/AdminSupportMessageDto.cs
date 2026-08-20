namespace VebTur.Application.Contracts.Admin;

/// <summary>Same shape for both the list row and the detail view — the message body is short enough that no separate summary/detail split is needed (unlike the Hotel aggregate).</summary>
public record AdminSupportMessageDto(
    Guid Id,
    string SenderName,
    string SenderEmail,
    string Subject,
    string Message,
    string? ReplyMessage,
    DateTime? RepliedAtUtc,
    DateTime CreatedAtUtc);
