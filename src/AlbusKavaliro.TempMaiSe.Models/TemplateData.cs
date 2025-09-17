using System.Collections.ObjectModel;

namespace AlbusKavaliro.TempMaiSe.Models;

public class TemplateData
{
    public MailAddress? From { get; set; }

    public Collection<MailAddress> To { get; } = [];

    public Collection<MailAddress> Cc { get; } = [];

    public Collection<MailAddress> Bcc { get; } = [];

    public Collection<MailAddress> ReplyTo { get; } = [];

    public Collection<Tag> Tags { get; } = [];

    public Collection<Header> Headers { get; } = [];

    public required string SubjectTemplate { get; set; }

    public string? HtmlBodyTemplate { get; set; }

    public string? PlainTextBodyTemplate { get; set; }

    public Priority Priority { get; set; } = Priority.None;

    public Collection<Attachment> Attachments { get; } = [];

    public Collection<Attachment> InlineAttachments { get; } = [];

    public required string JsonSchema { get; set; }
}
