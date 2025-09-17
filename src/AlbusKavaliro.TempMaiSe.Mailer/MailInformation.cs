using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Mailer;

public class MailInformation
{
    public string? From { get; init; }

    public ICollection<string> To { get; init; } = [];

    public ICollection<string> Cc { get; init; } = [];

    public ICollection<string> Bcc { get; init; } = [];

    public ICollection<string> ReplyTo { get; init; } = [];

    public ICollection<string> Tags { get; init; } = [];

    public ICollection<Header> Headers { get; init; } = [];

    public Priority? Priority { get; init; }

    public ICollection<Attachment> Attachments { get; init; } = [];

    public ICollection<Attachment> InlineAttachments { get; init; } = [];

    public dynamic? Data { get; init; }
}
