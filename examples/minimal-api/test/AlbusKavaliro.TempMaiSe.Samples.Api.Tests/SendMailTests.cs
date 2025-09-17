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
public class SendMailTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public SendMailTests(CustomWebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task Post_Send_Returns_Not_Found_For_Random_Template_Id()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(new Uri("/send/4711", UriKind.Relative), "dummy").ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_Id_Simple_Object()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 42,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "Inheritance from Uncle {{ uncle }}",
                            Priority = Priority.High,
                            Bcc = {
                                new() { Address= "prince@example.org" }
                            },
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

        MailInformation mail = new()
        {
            From = "government@example.org",
            ReplyTo = ["lawyer@example.com"],
            To = ["please-scam-me@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>()
            {
                { "email", "paypal@example.net" },
                { "uncle", "Bob" }
            }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/42", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Inheritance from Uncle Bob", message.Subject);
        Assert.Equal("Please send me 1.000 $. My paypal is paypal@example.net", message.TextBody);
        Assert.Null(message.HtmlBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "government@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "please-scam-me@example.com");

        Assert.NotNull(message.Bcc);
        Assert.Contains(message.Bcc, address => address.Address == "prince@example.org");
    }

    [Fact]
    public async Task Post_Send_Returns_Error_For_Missing_Required_Property()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 69,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "Inheritance from Uncle {{ uncle }}",
                            Priority = Priority.High,
                            Bcc = {
                                new() { Address = "prince@example.org" }
                            },
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

        MailInformation mail = new()
        {
            From = "government@example.org",
            ReplyTo = ["lawyer@example.com"],
            To = ["please-scam-me@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>()
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/69", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        string error = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("Required properties are missing from object: email.", error, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_Id_Complex_Object()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 1337,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "{{ Head.Name }} — Claim your free gift now",
                            PlainTextBodyTemplate = "Use a HTML mailer, {{ Head.Name }}!",
                            HtmlBodyTemplate =
"""
<p>Hi {{Head.Name}},<br><br>
you received a coupon for a free gift. Please use it within the next 24 hours.</p>

<ul>
    {% for item in CouponData.Items %}
    <li>{{ item.Name }}</li>
    {% endfor %}
</ul>

<p>Best,<br>
{{ Head.SupportAgent }}</p>
""",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "Head": {
            "type": "object",
            "properties": {
                "Name": { "type": "string" },
                "SupportAgent": { "type": "string" }
            },
            "required": ["Name", "SupportAgent"]
        },
        "CouponData": {
            "type": "object",
            "Items": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "Name": { "type": "string" }
                    },
                    "required": ["Name"]
                }
            },
            "required": ["Items"]
        }
    },
    "required": ["Head", "CouponData"]
}
"""
                        }
                    });
                    context.SaveChanges();
                });
            }).CreateClient();

        List<CouponItem> couponItems =
        [
            new("AK <script>47"),
            new("M4A1-S")
        ];
        MailInformation mail = new()
        {
            From = "couponmanager@example.org",
            To = ["couponer@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>
            {
                { "Head", new Dictionary<string, object>{ { "Name", "Erika Mustermann" }, { "SupportAgent", "Neo" } } },
                { "CouponData", new Dictionary<string, object>{ { "Items", couponItems } } },
            }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/1337", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Erika Mustermann — Claim your free gift now", message.Subject);
        Assert.Equal(
"""
<p>Hi Erika Mustermann,<br><br>
you received a coupon for a free gift. Please use it within the next 24 hours.</p>

<ul>
    
    <li>AK &lt;script&gt;47</li>
    
    <li>M4A1-S</li>
    
</ul>

<p>Best,<br>
Neo</p>
""", message.HtmlBody);
        Assert.Equal("Use a HTML mailer, Erika Mustermann!", message.TextBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "couponmanager@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "couponer@example.com");
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_Id_Complex_Object_With_Attachment()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 420,
                        Data = new TemplateData
                        {
                            Attachments =
                            {
                                new() { FileName = "coupon.pdf", MediaType = "application/pdf", Data = Encoding.UTF8.GetBytes("dummy") }
                            },
                            SubjectTemplate = "{{ Head.Name }} — Claim your free gift now",
                            PlainTextBodyTemplate = "Use a HTML mailer, {{ Head.Name }}!",
                            HtmlBodyTemplate =
"""
<p>Hi {{Head.Name}},<br><br>
you received a coupon for a free gift. Please use it within the next 24 hours.</p>

<ul>
    {% for item in CouponData.Items %}
    <li>{{ item.Name }}</li>
    {% endfor %}
</ul>

<p>Best,<br>
{{ Head.SupportAgent }}</p>
""",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "Head": {
            "type": "object",
            "properties": {
                "Name": { "type": "string" },
                "SupportAgent": { "type": "string" }
            },
            "required": ["Name", "SupportAgent"]
        },
        "CouponData": {
            "type": "object",
            "Items": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "Name": { "type": "string" }
                    },
                    "required": ["Name"]
                }
            },
            "required": ["Items"]
        }
    },
    "required": ["Head", "CouponData"]
}
"""
                        }
                    });
                    context.SaveChanges();
                });
            }).CreateClient();

        List<CouponItem> couponItems =
        [
            new("AK <script>47"),
            new("M4A1-S")
        ];
        MailInformation mail = new()
        {
            From = "couponmanager@example.org",
            To = ["couponer@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>
            {
                { "Head", new Dictionary<string, object>{ { "Name", "Erika Mustermann" }, { "SupportAgent", "Neo" } } },
                { "CouponData", new Dictionary<string, object>{ { "Items", couponItems } } },
            },
            Attachments =
            {
                new() { FileName = "logo.png", MediaType = "image/png", Data = Encoding.UTF8.GetBytes("dummy2") }
            },
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/420", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Erika Mustermann — Claim your free gift now", message.Subject);
        Assert.Equal(
"""
<p>Hi Erika Mustermann,<br><br>
you received a coupon for a free gift. Please use it within the next 24 hours.</p>

<ul>
    
    <li>AK &lt;script&gt;47</li>
    
    <li>M4A1-S</li>
    
</ul>

<p>Best,<br>
Neo</p>
""", message.HtmlBody);
        Assert.Equal("Use a HTML mailer, Erika Mustermann!", message.TextBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "couponmanager@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "couponer@example.com");

        Assert.Contains(message.Sections, section => section.MediaType == "application/pdf" && section.FileName == "coupon.pdf");
        Assert.Contains(message.Sections, section => section.MediaType == "image/png" && section.FileName == "logo.png");
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_Id_Complex_Object_With_Inline_Attachment()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 31337,
                        Data = new TemplateData
                        {
                            InlineAttachments =
                            {
                                new() { FileName = "blue.svg", MediaType = "image/svg+xml", Data = Encoding.UTF8.GetBytes("<svg style=\"background-color: blue;\"></svg>") },
                                new() { FileName = "red.svg", MediaType = "image/svg+xml", Data = Encoding.UTF8.GetBytes("<svg style=\"background-color: red;\"></svg>") }
                            },
                            SubjectTemplate = "{{ Head.Name }} — Claim your free gift now",
                            PlainTextBodyTemplate = "Use a HTML mailer, {{ Head.Name }}!",
                            HtmlBodyTemplate =
"""
<p>Hi {{Head.Name}},<br><br>
you received a coupon for a free gift. Please use it within the next 24 hours.</p>

<ul>
    {% for item in CouponData.Items %}
    <li>{{ item.Name }}</li>
    {% endfor %}
</ul>

<p>Best,<br>
{{ Head.SupportAgent }}</p>

<p><img src="{% inline_image "blue.svg" %}"></p>
<p><img src="{% inline_image "blue2.svg" %}"></p>
<p><img src="{% inline_image "red.svg" %}"></p>
{% if mail has_inline_image "yellow.svg" %}
<p><img src="{% inline_image "yellow.svg" %}"></p>
{% endif %}
{% if mail has_inline_image "magenta.svg" %}
<p><img src="{% inline_image "magenta.svg" %}"></p>
{% endif %}
""",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "Head": {
            "type": "object",
            "properties": {
                "Name": { "type": "string" },
                "SupportAgent": { "type": "string" }
            },
            "required": ["Name", "SupportAgent"]
        },
        "CouponData": {
            "type": "object",
            "Items": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "Name": { "type": "string" }
                    },
                    "required": ["Name"]
                }
            },
            "required": ["Items"]
        }
    },
    "required": ["Head", "CouponData"]
}
"""
                        }
                    });
                    context.SaveChanges();
                });
            }).CreateClient();

        List<CouponItem> couponItems =
        [
            new("AK <script>47"),
            new("M4A1-S")
        ];
        MailInformation mail = new()
        {
            From = "couponmanager@example.org",
            To = ["couponer@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>
            {
                { "Head", new Dictionary<string, object>{ { "Name", "Erika Mustermann" }, { "SupportAgent", "Neo" } } },
                { "CouponData", new Dictionary<string, object>{ { "Items", couponItems } } },
            },
            InlineAttachments =
            {
                new() { FileName = "blue2.svg", MediaType = "image/svg+xml", Data = Encoding.UTF8.GetBytes("<svg style=\"background-color: blue;\"></svg>") },
                new() { FileName = "yellow.svg", MediaType = "image/svg+xml", Data = Encoding.UTF8.GetBytes("<svg style=\"background-color: yellow;\"></svg>") }
            }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/31337", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Erika Mustermann — Claim your free gift now", message.Subject);
        Assert.Equal(
"""
<p>Hi Erika Mustermann,<br><br>
you received a coupon for a free gift. Please use it within the next 24 hours.</p>

<ul>
    
    <li>AK &lt;script&gt;47</li>
    
    <li>M4A1-S</li>
    
</ul>

<p>Best,<br>
Neo</p>

<p><img src="cid:8e782df90bdb749881b592be981befc7ba1320536621e540b836a29d35d5a4d9"></p>
<p><img src="cid:8e782df90bdb749881b592be981befc7ba1320536621e540b836a29d35d5a4d9"></p>
<p><img src="cid:a101746ce2666bca288096e79b43e2b8099ded9947ecb30f1623e3009644ee31"></p>

<p><img src="cid:ffd7bca49b22eb931895418b84d0408853b3ff2e4d0704738dd8e5f56e16f252"></p>


""", message.HtmlBody);
        Assert.Equal("Use a HTML mailer, Erika Mustermann!", message.TextBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "couponmanager@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "couponer@example.com");

        Assert.Contains(message.Sections, section => section.MediaType == "image/svg+xml" && section.FileName == "blue.svg" && section.Id == "8e782df90bdb749881b592be981befc7ba1320536621e540b836a29d35d5a4d9");
        Assert.Contains(message.Sections, section => section.MediaType == "image/svg+xml" && section.FileName == "red.svg" && section.Id == "a101746ce2666bca288096e79b43e2b8099ded9947ecb30f1623e3009644ee31");
        Assert.Contains(message.Sections, section => section.MediaType == "image/svg+xml" && section.FileName == "yellow.svg" && section.Id == "ffd7bca49b22eb931895418b84d0408853b3ff2e4d0704738dd8e5f56e16f252");
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_With_Custom_Fluid_Tag()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 31338,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "{{ Head.Name }} — Claim your free gift now",
                            PlainTextBodyTemplate = "Use a HTML mailer, {{ Head.Name }}!",
                            HtmlBodyTemplate =
"""
<p>{% dummy %}</p>
""",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "Head": {
            "type": "object",
            "properties": {
                "Name": { "type": "string" },
                "SupportAgent": { "type": "string" }
            },
            "required": ["Name", "SupportAgent"]
        }
    },
    "required": ["Head"]
}
"""
                        }
                    });
                    context.SaveChanges();
                });
            }).CreateClient();

        MailInformation mail = new()
        {
            From = "couponmanager@example.org",
            To = ["couponer@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>
            {
                { "Head", new Dictionary<string, object>{ { "Name", "Erika Mustermann" }, { "SupportAgent", "Neo" } } }
            }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/31338", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Erika Mustermann — Claim your free gift now", message.Subject);
        Assert.Equal("""<p><img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAIAAAAmkwkpAAAAQ0lEQVR4nGKanbxR0iCpx+Lsq7MfGBKt0pls/206J81Y9Yrx7rta3mMP0lrvGci3MjNw/wj989bz5ksPzqOAAAAA//94+BjST+Y61wAAAABJRU5ErkJggg==" alt="Dummy"></p>""", message.HtmlBody);
        Assert.Equal("Use a HTML mailer, Erika Mustermann!", message.TextBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "couponmanager@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "couponer@example.com");
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_With_Custom_Fluid_Tag_As_Attachment()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 31339,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "{{ Head.Name }} — Claim your free gift now",
                            PlainTextBodyTemplate = "Use a HTML mailer, {{ Head.Name }}!",
                            HtmlBodyTemplate =
"""
<p>{% logo "magenta" %}</p>
""",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "Head": {
            "type": "object",
            "properties": {
                "Name": { "type": "string" },
                "SupportAgent": { "type": "string" }
            },
            "required": ["Name", "SupportAgent"]
        }
    },
    "required": ["Head"]
}
"""
                        }
                    });
                    context.SaveChanges();
                });
            }).CreateClient();

        MailInformation mail = new()
        {
            From = "couponmanager@example.org",
            To = ["couponer@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>
            {
                { "Head", new Dictionary<string, object>{ { "Name", "Erika Mustermann" }, { "SupportAgent", "Neo" } } }
            }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/31339", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Erika Mustermann — Claim your free gift now", message.Subject);
        Assert.Matches("""<p><img src="cid:[0-9a-z]{64}" alt="Logo"></p>""", message.HtmlBody);
        Assert.Equal("Use a HTML mailer, Erika Mustermann!", message.TextBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "couponmanager@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "couponer@example.com");

        Assert.Contains(message.Sections, section => section.MediaType == "image/svg+xml" && section.FileName == "logo.svg" && !string.IsNullOrWhiteSpace(section.Id));
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_Id_Simple_Object_Signature()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 0_0_7,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "Inheritance from Uncle {{ uncle }}",
                            Priority = Priority.High,
                            Bcc = {
                                new() { Address= "prince@example.org" }
                            },
                            PlainTextBodyTemplate = """
Please send me 1.000 $. My paypal is {{ email }}

{% partial 'signature', extension: extension %}
""",
                            HtmlBodyTemplate =
"""
<p>Please send me 1.000 $. My paypal is {{ email }}</p>

{% partial 'signature', extension: extension %}
""",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "email": { "type": "string", "format": "email" },
        "uncle": { "type": "string" },
        "extension": { "type": "string" }
    },
    "required": ["email"]
}
""",
                        }
                    });

                    context.Partials.Add(new Partial
                    {
                        Id = 666,
                        Key = "signature",
                        PlainTextTemplate =
"""
Best regards,
Dummy.

Phone +493023125{{ extension }}
""",
                        HtmlTemplate =
"""
<p>Best regards,<br>Dummy.</p>
<p>Tel: <a href="tel:+493023125{{ extension }}">+49 30 23125 {{ extension }}</a></p>
"""
                    });

                    context.SaveChanges();
                });
            }).CreateClient();

        MailInformation mail = new()
        {
            From = "government@example.org",
            ReplyTo = ["lawyer@example.com"],
            To = ["please-scam-me@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>()
            {
                { "email", "paypal@example.net" },
                { "uncle", "Bob" },
                { "extension", "666" }
            }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/7", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Inheritance from Uncle Bob", message.Subject);
        Assert.Equal(
"""
Please send me 1.000 $. My paypal is paypal@example.net

Best regards,
Dummy.

Phone +493023125666
""", message.TextBody);
        Assert.Equal(
"""
<p>Please send me 1.000 $. My paypal is paypal@example.net</p>

<p>Best regards,<br>Dummy.</p>
<p>Tel: <a href="tel:+493023125666">+49 30 23125 666</a></p>
""", message.HtmlBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "government@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "please-scam-me@example.com");

        Assert.NotNull(message.Bcc);
        Assert.Contains(message.Bcc, address => address.Address == "prince@example.org");
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_Id_Simple_Object_Signature_No_Parameter()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 0_0_9,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "Inheritance from Uncle {{ uncle }}",
                            Priority = Priority.High,
                            Bcc = {
                                new() { Address= "prince@example.org" }
                            },
                            PlainTextBodyTemplate = """
Please send me 1.000 $. My paypal is {{ email }}

{% partial 'salutation' %}
""",
                            HtmlBodyTemplate =
"""
<p>Please send me 1.000 $. My paypal is {{ email }}</p>

{% partial 'salutation' %}
""",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "email": { "type": "string", "format": "email" },
        "uncle": { "type": "string" },
        "extension": { "type": "string" }
    },
    "required": ["email"]
}
""",
                        }
                    });

                    context.Partials.Add(new Partial
                    {
                        Id = 420,
                        Key = "salutation",
                        PlainTextTemplate =
"""
Best regards,
Dummy.
""",
                        HtmlTemplate =
"""
<p>Best regards,<br>Dummy.</p>
"""
                    });

                    context.SaveChanges();
                });
            }).CreateClient();

        MailInformation mail = new()
        {
            From = "government@example.org",
            ReplyTo = ["lawyer@example.com"],
            To = ["please-scam-me@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>()
            {
                { "email", "paypal@example.net" },
                { "uncle", "Bob" }
            }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/9", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Inheritance from Uncle Bob", message.Subject);
        Assert.Equal(
"""
Please send me 1.000 $. My paypal is paypal@example.net

Best regards,
Dummy.
""", message.TextBody);
        Assert.Equal(
"""
<p>Please send me 1.000 $. My paypal is paypal@example.net</p>

<p>Best regards,<br>Dummy.</p>
""", message.HtmlBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "government@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "please-scam-me@example.com");

        Assert.NotNull(message.Bcc);
        Assert.Contains(message.Bcc, address => address.Address == "prince@example.org");
    }

    [Fact]
    public async Task Post_Send_Returns_Success_For_Good_Template_Id_Simple_Object_Signature_With_Inline_Image()
    {
        // Arrange
        PapercutContainer container = new PapercutBuilder()
            .Build();
        await container.StartAsync().ConfigureAwait(true);

        HttpClient client = _factory
            .WithWebHostBuilder(configuration =>
            {
                configuration.UseSetting("FluentEmail:Sender", "Smtp");
                configuration.UseSetting("FluentEmail:Smtp:Server", container.Hostname);
                configuration.UseSetting("FluentEmail:Smtp:Port", container.SmtpPort.ToString(CultureInfo.InvariantCulture));

                configuration.ConfigureTestServices(services =>
                {
                    using TemplateContext context = services.BuildServiceProvider().GetRequiredService<TemplateContext>();
                    context.Templates.Add(new Template
                    {
                        Id = 0_1_0,
                        Data = new TemplateData
                        {
                            SubjectTemplate = "Inheritance from Uncle {{ uncle }}",
                            Priority = Priority.High,
                            Bcc = {
                                new() { Address= "prince@example.org" }
                            },
                            PlainTextBodyTemplate = """
Please send me 1.000 $. My paypal is {{ email }}

{% partial 'signature_with_company' %}
""",
                            HtmlBodyTemplate =
"""
<p>Please send me 1.000 $. My paypal is {{ email }}</p>

{% partial 'signature_with_company' %}
""",
                            JsonSchema =
"""
{
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
        "email": { "type": "string", "format": "email" },
        "uncle": { "type": "string" },
        "extension": { "type": "string" }
    },
    "required": ["email"]
}
""",
                        }
                    });

                    context.Partials.Add(new Partial
                    {
                        Id = 65535,
                        Key = "signature_with_company",
                        PlainTextTemplate =
"""
Best regards,
Dummy.
""",
                        HtmlTemplate =
"""
<p>Best regards,<br>Dummy.</p>
<p><img src="{% inline_image "company.svg" %}" alt="Company"></p>
""",
                        InlineAttachments =
                        {
                            new() { FileName = "company.svg", MediaType = "image/svg+xml", Data = Encoding.UTF8.GetBytes("<svg style=\"background-color: blue;\"></svg>") }
                        }
                    });

                    context.SaveChanges();
                });
            }).CreateClient();

        MailInformation mail = new()
        {
            From = "government@example.org",
            ReplyTo = ["lawyer@example.com"],
            To = ["please-scam-me@example.com"],
            Priority = Priority.Normal,
            Data = new Dictionary<string, object>()
            {
                { "email", "paypal@example.net" },
                { "uncle", "Bob" }
            }
        };

        // Act
        using HttpContent content = new StringContent(JsonSerializer.Serialize(mail), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(new Uri("/send/10", UriKind.Relative), content).ConfigureAwait(true);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(container.GetBaseAddress());
        PapercutMessageList? messages = await httpClient.GetFromJsonAsync<PapercutMessageList>(new Uri("/api/messages", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(1, messages!.TotalMessageCount);
        PapercutMessage? message = await httpClient.GetFromJsonAsync<PapercutMessage>(new Uri($"/api/messages/{messages.Messages.Single().Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(message);
        Assert.Equal("Inheritance from Uncle Bob", message.Subject);
        Assert.Equal(
"""
Please send me 1.000 $. My paypal is paypal@example.net

Best regards,
Dummy.
""", message.TextBody);
        Assert.Equal(
"""
<p>Please send me 1.000 $. My paypal is paypal@example.net</p>

<p>Best regards,<br>Dummy.</p>
<p><img src="cid:8e782df90bdb749881b592be981befc7ba1320536621e540b836a29d35d5a4d9" alt="Company"></p>
""", message.HtmlBody);

        Assert.NotNull(message.From);
        Assert.Contains(message.From, address => address.Address == "government@example.org");

        Assert.NotNull(message.To);
        Assert.Contains(message.To, address => address.Address == "please-scam-me@example.com");

        Assert.NotNull(message.Bcc);
        Assert.Contains(message.Bcc, address => address.Address == "prince@example.org");
    }

    private sealed record CouponItem(string Name);
}
