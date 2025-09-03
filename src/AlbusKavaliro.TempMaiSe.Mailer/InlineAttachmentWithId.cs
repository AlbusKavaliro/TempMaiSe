using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// Represents an attachment with an ID that can be used for inline display in an email.
/// </summary>
/// <summary>
/// Represents an attachment with an associated ID.
/// </summary>
internal sealed record InlineAttachmentWithId(InlineAttachmentId Id, Attachment Attachment);
