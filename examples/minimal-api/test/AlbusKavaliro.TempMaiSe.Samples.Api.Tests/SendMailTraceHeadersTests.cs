using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;

using AlbusKavaliro.TempMaiSe.Mailer;
using AlbusKavaliro.TempMaiSe.Models;

using Testcontainers.Papercut;

namespace AlbusKavaliro.TempMaiSe.Samples.Api.Tests;

[Trait("Category", "Integration")]
public class SendMailTraceHeadersTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TraceHeadersFeatureName = "TempMaiSe.InjectTraceHeaders";

    private readonly CustomWebApplicationFactory<Program> _factory;

    public SendMailTraceHeadersTests(CustomWebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task Post_Send_Includes_Traceparent_Header_When_Feature_Is_Enabled()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder("docker.io/changemakerstudiosus/papercut-smtp:7.7")
            .Build();
        await container.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        HttpClient client = CreateClient(container, templateId: 9001, injectTraceHeaders: true);

        MailInformation mail = new()
        {
            From = "government@example.org",
            To = ["please-scam-me@example.com"],
            Data = new Dictionary<string, object> { { "email", "paypal@example.net" } }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/9001", UriKind.Relative), content, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PapercutMessage sentMail = await GetSingleMessageAsync(container).ConfigureAwait(true);
        Assert.Contains(sentMail.Headers, h => string.Equals(h.Name, "traceparent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Post_Send_Does_Not_Include_Trace_Headers_When_Feature_Is_Disabled()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder("docker.io/changemakerstudiosus/papercut-smtp:7.7")
            .Build();
        await container.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        HttpClient client = CreateClient(container, templateId: 9002, injectTraceHeaders: false);

        MailInformation mail = new()
        {
            From = "government@example.org",
            To = ["please-scam-me@example.com"],
            Data = new Dictionary<string, object> { { "email", "paypal@example.net" } }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/9002", UriKind.Relative), content, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PapercutMessage sentMail = await GetSingleMessageAsync(container).ConfigureAwait(true);
        Assert.DoesNotContain(sentMail.Headers, h => string.Equals(h.Name, "traceparent", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(sentMail.Headers, h => string.Equals(h.Name, "tracestate", StringComparison.OrdinalIgnoreCase));
    }

    private HttpClient CreateClient(PapercutContainer container, int templateId, bool injectTraceHeaders)
    {
        return _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));
                configuration.UseSetting($"FeatureManagement:{TraceHeadersFeatureName}", injectTraceHeaders ? "true" : "false");

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = templateId,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "Inheritance from Uncle {{ uncle }}",
                            PlainTextBodyTemplate = "Please send me 1.000 $. My paypal is {{ email }}",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "email": { "type": "string", "format": "email" },
        "uncle": { "type": "string" }
    },
    "required": ["email"]
}
"""
                        }
                    });
                    context.SaveChanges();
                });
            }).CreateClient();
    }

    private static async Task<PapercutMessage> GetSingleMessageAsync(PapercutContainer container)
    {
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative), TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(messages);
        Assert.Equal(1, messages.TotalMessageCount);

        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative), TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(message);
        return message;
    }
}