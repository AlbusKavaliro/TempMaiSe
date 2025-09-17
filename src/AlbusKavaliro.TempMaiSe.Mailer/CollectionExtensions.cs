using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Mailer;

internal static class CollectionExtensions
{
    /// <summary>
    /// Adds a range of attachments to the <see cref="InlineAttachmentCollection"/>.
    /// </summary>
    /// <param name="collection">The <see cref="InlineAttachmentCollection"/> to add the attachments to.</param>
    /// <param name="items">The collection of attachments to add.</param>
    public static void CopyTo(this ICollection<Attachment> items, InlineAttachmentCollection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(items);

        collection.AddRange(items);
    }
}
