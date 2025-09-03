using System.Text;
using Newtonsoft.Json.Schema;
using AlbusKavaliro.TempMaiSe.Mailer;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Tests;

[Trait("Category", "Unit")]
public class DataParserTests
{
    private readonly DataParser _parser = new();

    [Theory]
    [InlineData(" ")]
    public async Task ParseAsync_Throws_ArgumentException_When_Given_No_Schema(string jsonSchema)
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(nameof(jsonSchema), () => _parser.ParseAsync(jsonSchema, stream)).ConfigureAwait(true);
    }

    [Fact]
    public async Task ParseAsync_Throws_ArgumentNullException_When_Given_Null_Schema()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>("jsonSchema", () => _parser.ParseAsync(null, stream)).ConfigureAwait(true);
    }

    [Fact]
    public async Task ParseAsync_Throws_ArgumentNullException_When_Given_No_Data()
    {
        // Arrange
        const string jsonSchema = "{}";

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>("data", () => _parser.ParseAsync(jsonSchema, null)).ConfigureAwait(true);
    }

    [Fact]
    public async Task ParseAsync_Returns_Error_If_Data_Does_Not_Match_Schema()
    {
        // Arrange
        const string jsonSchema = """
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "type": "object",
            "properties": {
                "email": { "type": "string", "format": "email" }
            },
            "required": ["email"]
        }
        """;

        using Stream data = new MemoryStream();
        using StreamWriter writer = new(data, encoding: Encoding.UTF8, leaveOpen: true);
        await writer.WriteAsync("""
        {
            "From": "dummy@example.org",
            "To": ["test@example.org"],
            "Cc": ["test@example.org"],
            "Bcc": ["test@example.org"],
            "ReplyTo": ["test@example.org"],
            "Tags": ["dummy"],
            "Headers": [],
            "Attachments": [],
            "InlineAttachments": [],
            "Priority": 1,
            "Data": {
                "email": "foo"
            }
        }
        """).ConfigureAwait(true);
        await writer.FlushAsync().ConfigureAwait(true);
        data.Position = 0;

        // Act
        OneOf.OneOf<MailInformation, List<ValidationError>> mailInformationOrError = await _parser.ParseAsync(jsonSchema, data).ConfigureAwait(true);

        // Assert
        Assert.False(mailInformationOrError.IsT0);
        Assert.NotNull(mailInformationOrError.AsT1);
        Assert.Single(mailInformationOrError.AsT1);
    }

    [Fact]
    public async Task ParseAsync_Returns_Template_If_Data_Does_Match_Schema()
    {
        // Arrange
        const string jsonSchema = """
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "type": "object",
            "properties": {
                "email": { "type": "string", "format": "email" }
            },
            "required": ["email"]
        }
        """;

        using Stream data = new MemoryStream();
        using StreamWriter writer = new(data, encoding: Encoding.UTF8, leaveOpen: true);
        await writer.WriteAsync("""
        {
            "From": "dummy@example.org",
            "To": ["to@example.org"],
            "Cc": ["cc@example.org"],
            "Bcc": ["bcc@example.org"],
            "ReplyTo": ["replyTo@example.org"],
            "Tags": ["dummy"],
            "Attachments": [
                {
                    "FileName": "file.txt",
                    "MediaType": "text/plain",
                    "Data": "SGVsbG8gV29ybGQ="
                }
            ],
            "InlineAttachments": [],
            "Headers": [
                {
                    "Name": "traceparent",
                    "Value": "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"
                }
            ],
            "Priority": 3,
            "Data": {
                "email": "foo@example.org"
            }
        }
        """).ConfigureAwait(true);
        await writer.FlushAsync().ConfigureAwait(true);
        data.Position = 0;

        // Act
        OneOf.OneOf<MailInformation, List<ValidationError>> mailInformationOrError = await _parser.ParseAsync(jsonSchema, data).ConfigureAwait(true);

        // Assert
        Assert.True(mailInformationOrError.IsT0);
        Assert.False(mailInformationOrError.IsT1);
        MailInformation mailInformation = mailInformationOrError.AsT0;
        Assert.NotNull(mailInformation);
        Assert.Equal("dummy@example.org", mailInformation.From);
        Assert.Equal("to@example.org", mailInformation.To.Single());
        Assert.Equal("cc@example.org", mailInformation.Cc.Single());
        Assert.Equal("bcc@example.org", mailInformation.Bcc.Single());
        Assert.Equal("replyTo@example.org", mailInformation.ReplyTo.Single());
        Assert.Equal("dummy", mailInformation.Tags.Single());
        Assert.Equal(Priority.Low, mailInformation.Priority);
        Assert.Equal("foo@example.org", (string)mailInformation.Data!.email);
        Assert.Contains(mailInformation.Attachments, a => a.FileName == "file.txt" && a.MediaType == "text/plain" && a.Data.Length == 11);
        Assert.Contains(mailInformation.Headers, h => h.Name == "traceparent" && h.Value == "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
    }
}
