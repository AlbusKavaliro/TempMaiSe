using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using OneOf;

namespace AlbusKavaliro.TempMaiSe.Mailer;

public class DataParser : IDataParser
{
    private static readonly JSchema s_templateSchema = new JSchemaGenerator().Generate(typeof(MailInformation));

    /// <inheritdoc />
    public async Task<OneOf<MailInformation, List<ValidationError>>> ParseAsync(string jsonSchema, Stream data, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonSchema);
        ArgumentNullException.ThrowIfNull(data);

        JSchema templateSchema = CloneTemplateSchema();
        JSchema dataSchema = JSchema.Parse(jsonSchema);
        templateSchema.Properties["Data"] = dataSchema;

        using var sr = new StreamReader(data, Encoding.UTF8);
        string str = await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        using JsonTextReader reader = new(new StringReader(str));

        using JSchemaValidatingReader validatingReader = new(reader)
        {
            Schema = templateSchema
        };

        List<ValidationError> errors = [];
        validatingReader.ValidationEventHandler += (o, a) => errors.Add(a.ValidationError);

        JsonSerializer serializer = new();
        MailInformation mailInformation = serializer.Deserialize<MailInformation>(validatingReader)!;
        return errors.Count > 0 ? (OneOf<MailInformation, List<ValidationError>>)errors : (OneOf<MailInformation, List<ValidationError>>)mailInformation;
    }

    private static JSchema CloneTemplateSchema() => JSchema.Parse(s_templateSchema.ToString());
}
