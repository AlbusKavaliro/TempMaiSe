using System.Collections;
using System.Diagnostics.CodeAnalysis;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// Deduplicates inline attachments and provides methods to look up the attachment ID.
/// </summary>
internal sealed class InlineAttachmentCollection : IEnumerable<InlineAttachmentWithId>
{
    private readonly Dictionary<string, InlineAttachmentWithId> _attachmentByFileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineAttachmentCollection"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the collection.</param>
    public InlineAttachmentCollection(int capacity)
    {
        _attachmentByFileName = new Dictionary<string, InlineAttachmentWithId>(capacity);
    }

    /// <summary>
    /// Adds a range of attachments to the collection.
    /// </summary>
    /// <param name="attachments">The attachments to add.</param>
    public void AddRange(IEnumerable<Attachment> attachments)
    {
        ArgumentNullException.ThrowIfNull(attachments);

        foreach (Attachment attachment in attachments)
        {
            Add(attachment);
        }
    }

    /// <summary>
    /// Adds an attachment to the collection.
    /// </summary>
    /// <param name="attachment">The attachment to add.</param>
    /// <returns>The ID of the added attachment.</returns>
    public InlineAttachmentWithId Add(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        InlineAttachmentId attachmentId = new(attachment);
        InlineAttachmentWithId attachmentWithId = new(attachmentId, attachment);

        _attachmentByFileName[attachment.FileName] = attachmentWithId;
        return attachmentWithId;
    }

    /// <summary>
    /// Tries to get the attachment by its file name.
    /// </summary>
    /// <param name="fileName">The file name of the attachment.</param>
    /// <param name="attachment">When this method returns, contains the attachment with the specified file name, if found; otherwise, null.</param>
    /// <returns>true if the attachment with the specified file name is found; otherwise, false.</returns>
    /// <remarks>Filenames are cse-insensitive.</remarks>
    public bool TryGetAttachmentByFileName(string fileName, [NotNullWhen(true)] out InlineAttachmentWithId? attachment)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        return _attachmentByFileName.TryGetValue(fileName, out attachment);
    }

    /// <inheritdoc />
    public IEnumerator<InlineAttachmentWithId> GetEnumerator() => _attachmentByFileName.Values.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _attachmentByFileName.Values.GetEnumerator();
}
