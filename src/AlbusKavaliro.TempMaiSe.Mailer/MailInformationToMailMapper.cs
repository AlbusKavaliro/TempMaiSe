using FluentEmail.Core;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Mailer;

public class MailInformationToMailMapper : IMailInformationToMailMapper
{
    public IFluentEmail Map(MailInformation mailInformation, IFluentEmail email)
    {
        ArgumentNullException.ThrowIfNull(mailInformation);
        ArgumentNullException.ThrowIfNull(email);

        if (!string.IsNullOrWhiteSpace(mailInformation.From))
        {
            email = email.SetFrom(mailInformation.From);
        }

        foreach (string to in mailInformation.To)
        {
            email = email.To(to);
        }

        foreach (string cc in mailInformation.Cc)
        {
            email = email.CC(cc);
        }

        foreach (string bcc in mailInformation.Bcc)
        {
            email = email.BCC(bcc);
        }

        foreach (string replyTo in mailInformation.ReplyTo)
        {
            email = email.ReplyTo(replyTo);
        }

        foreach (string tag in mailInformation.Tags)
        {
            email = email.Tag(tag);
        }

        foreach (Header header in mailInformation.Headers)
        {
            email = email.Header(header.Name, header.Value);
        }

        switch (mailInformation.Priority)
        {
            case Priority.High:
                email = email.HighPriority();
                break;
            case Priority.Low:
                email = email.LowPriority();
                break;
        }

        if (mailInformation.Attachments is not null)
        {
            foreach (Attachment attachment in mailInformation.Attachments)
            {
                FluentEmail.Core.Models.Attachment fluentAttachment = new() { Filename = attachment.FileName, ContentType = attachment.MediaType, Data = new MemoryStream(attachment.Data), IsInline = false };
                email = email.Attach(fluentAttachment);
            }
        }

        return email;
    }
}
