namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// Represents an interface for fluid extensibility.
/// </summary>
public interface IFluidExtensibility
{
    /// <summary>
    /// Adds an inline attachment to the email.
    /// </summary>
    /// <param name="fileName">The name of the attachment file.</param>
    /// <param name="data">The content of the attachment as a byte array.</param>
    /// <param name="mediaType">The media type of the attachment.</param>
    /// <returns>The unique identifier of the attachment.</returns>
    string AddInlineAttachment(string fileName, byte[] data, string mediaType);
}
