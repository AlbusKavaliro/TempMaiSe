using Fluid;
using Fluid.Ast;
using static Parlot.Fluent.Parsers;

namespace AlbusKavaliro.TempMaiSe.Mailer;

public class FluidMailParser : FluidParser
{
    public FluidMailParser(FluidParserOptions parserOptions) : base(parserOptions)
    {
        RegisteredOperators["has_inline_image"] = (a, b) => new HasInlineImageBinaryExpression(a, b);
        RegisterExpressionTag("inline_image", InlineImageTag.WriteToAsync);

        var partialExpression = Parlot.Fluent.Parsers.OneOf(
                         Primary.AndSkip(Comma).And(Separated(Comma, Identifier.AndSkip(Colon).And(Primary).Then(static x => new AssignStatement(x.Item1, x.Item2)))).Then(x => new { Expression = x.Item1, Assignments = x.Item2 }),
                         Primary.Then(x => new { Expression = x, Assignments = (IReadOnlyList<AssignStatement>)[] })
                         ).ElseError("Invalid 'partial' tag");

        RegisterParserTag("partial", partialExpression, async (partialStatement, writer, encoder, context) =>
        {
            return await PartialTag.WriteToAsync(partialStatement.Expression, writer, encoder, context).ConfigureAwait(false);
        });
    }
}
