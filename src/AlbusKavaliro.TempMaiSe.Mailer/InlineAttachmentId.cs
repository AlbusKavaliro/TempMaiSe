using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// Represents an identifier for an inline attachment.
/// </summary>
internal sealed class InlineAttachmentId
{
    private readonly string _attachmentId;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineAttachmentId"/> class.
    /// </summary>
    /// <param name="attachment">The attachment to generate the identifier for.</param>
    public InlineAttachmentId(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        _attachmentId = GetAttachmentId(attachment);
    }

    /// <summary>
    /// Implicitly converts an <see cref="InlineAttachmentId"/> to a string.
    /// </summary>
    /// <param name="id">The <see cref="InlineAttachmentId"/> to convert.</param>
    /// <returns>The string representation of the <see cref="InlineAttachmentId"/>.</returns>
    public static implicit operator string(InlineAttachmentId id) => id._attachmentId;

    /// <inheritdoc />
    public override string ToString() => _attachmentId;

    private static string GetAttachmentId(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        bool success = SHA256.TryHashData(attachment.Data, hash, out int bytesWritten);
        if (!success)
        {
            return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }

        StringBuilder builder = new(SHA256.HashSizeInBytes * 2);
        for (int i = 0; i < hash.Length; i++)
        {
            builder = builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
