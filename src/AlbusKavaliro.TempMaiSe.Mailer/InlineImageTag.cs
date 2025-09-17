using System.Text.Encodings.Web;
using Fluid.Ast;
using Fluid.Values;

namespace AlbusKavaliro.TempMaiSe.Mailer;

internal static class InlineImageTag
{
    public static async ValueTask<Completion> WriteToAsync(Expression value, TextWriter writer, TextEncoder encoder, Fluid.TemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(context);

        if (context.AmbientValues.TryGetValue(nameof(InlineAttachmentCollection), out InlineAttachmentCollection? inlineAttachments) is not true)
        {
            return Completion.Normal;
        }

        FluidValue fluidValue = await value.EvaluateAsync(context).ConfigureAwait(false);
        if (!inlineAttachments!.TryGetAttachmentByFileName(fluidValue.ToStringValue(), out InlineAttachmentWithId? attachment))
        {
            return Completion.Normal;
        }

        await writer.WriteAsync($"cid:{attachment.Id}").ConfigureAwait(false);
        return Completion.Normal;
    }
}
