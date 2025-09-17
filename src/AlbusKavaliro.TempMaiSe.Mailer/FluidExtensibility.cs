using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Mailer;

internal sealed class FluidExtensibility(InlineAttachmentCollection inlineAttachments) : IFluidExtensibility
{
    /// <inheritdoc />
    public string AddInlineAttachment(string fileName, byte[] data, string mediaType)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(mediaType);

        Attachment attachment = new() { FileName = fileName, Data = data, MediaType = mediaType };

        InlineAttachmentWithId attachmentWithId = inlineAttachments.Add(attachment);
        return attachmentWithId.Id;
    }
}
