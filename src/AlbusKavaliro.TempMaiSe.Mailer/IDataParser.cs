using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Schema;

namespace AlbusKavaliro.TempMaiSe.Mailer;

public interface IDataParser
{
    /// <summary>
    /// Parses the given data stream with the given JSON schema.
    /// </summary>
    /// <param name="jsonSchema">The JSON schema for mailing data.</param>
    /// <param name="data">The actual data to apply to the template.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to abort parsing.</param>
    /// <returns>
    /// Either an instance of <see cref="MailInformation"/> representing the
    /// deserialized mail data, or a list of <see cref="ValidationError"/>, if
    /// any problems occur during parsing of the data.
    /// </returns>
    Task<ParseResult> ParseAsync(string jsonSchema, Stream data, CancellationToken cancellationToken = default);
}

public readonly union ParseResult(MailInformation, List<ValidationError>)
{
    public bool Evaluate([NotNullWhen(true)] out MailInformation? mailInformation, [NotNullWhen(false)] out List<ValidationError>? validationErrors)
    {
        if (this is MailInformation mi)
        {
            mailInformation = mi;
            validationErrors = null;
            return true;
        }

        if (this is List<ValidationError> ve)
        {
            mailInformation = null;
            validationErrors = ve;
            return false;
        }

        throw new InvalidOperationException("Unexpected parse result type.");
    }
}
