# TempMaiSe

TempMaiSe is an open-source library designed to simplify the process of sending templated emails using the [Fluid](https://github.com/sebastienros/fluid) templating engine and the [FluentEmail](https://github.com/lukencode/FluentEmail) sender. It provides a convenient and intuitive way to generate dynamic email content.

## Features

-   Easy integration with FluentEmail sender
-   Support for Fluid templating engine
-   Simplified email template retrieval with custom repository implementation

## Installation

To use the TempMaiSe library in your project, follow these steps:

1. Add a reference to the [`AlbusKavaliro.TempMaiSe.Mailer`](https://www.nuget.org/packages/AlbusKavaliro.Mailer) NuGet package.

## Usage

To implement a custom repository and use the TempMaiSe library, follow these steps:

1. Create a class that implements the `ITemplateRepository` interface.
2. Implement the `GetTemplateAsync` method to retrieve the template based on the provided ID.
3. If you want to use Partials, also implement the `IPartialRepository` interface, and
4. Implement the `GetPartialAsync` method to retrieve the partial based on the provided key.
5. Register the custom repositories in your project's `Startup.cs` file.
6. Register the necessary components in your `Startup.cs` file, including the custom repository, FluentEmail setup, and the default IMailService implementation.
7. Utilize the provided API sample to send an email using the configured mail service.

Please note that additional authentication and authorization features may need to be implemented according to your specific requirements.

For more information, refer to the [TempMaiSe documentation](https://github.com/AlbusKavaliro/TempMaiSe).

### Custom Repository

The TempMaiSe distribution does not come with a default repository. However, implementing a custom repository is straightforward.

To implement a custom repository, follow these steps:

1. Create a class that implements the `ITemplateRepository` interface.
2. Implement the `GetTemplateAsync` method to retrieve the template based on the provided ID.
3. Create a class that implements the `IPartialRepository` interface.
4. Implement the `GetPartialAsync` method to retrieve the template based on the provided key.
5. Register the custom repositories in your project's `Startup.cs` file.

Here's an example of a custom repository that only returns one `Template`.

```csharp
public class TemplateRepository : ITemplateRepository
{
    private static readonly s_theOneTemplate = new Template
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
                    };

    /// <inheritdoc />
    public Task<Template?> GetTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        Template? result;
        if (id == s_theOneTemplate.Id)
        {
            result = s_theOneTemplate;
        }

        return Task.FromResult(result);
    }
}
```

### Registering Components in `Startup.cs`

To use the TempMaiSe library in your project, you need to register the necessary components in your `Startup.cs` file. Here's how you can do it:

```csharp
// Your custom repository
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IPartialRepository, PartialRepository>();

// Use the usual FluentEmail setup to register IFluentEmail
builder.Services.AddFluentEmail(config);

// Register the default IMailService implementation
builder.Services.AddMailService();
```

### Example REST-API

To send an email using the configured mail service, you can utilize the following API sample. It is important to note that this sample does not include authentication or authorization, so it is recommended to implement those features according to your specific requirements.

```csharp
app.MapPost("/send/{id}", async (int id, Stream data, IMailService mailService, CancellationToken cancellationToken) =>
{
    static IDictionary<string, string[]> ConvertValidationErrorsToValidationProblem(List<ValidationError> validationErrors)
    {
        return validationErrors
            .GroupBy(error => error.Path)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).ToArray()
            );
    }

    OneOf<FluentEmail.Core.Models.SendResponse, NotFound, List<ValidationError>> result = await mailService.SendMailAsync(id, data, cancellationToken).ConfigureAwait(false);

    return result.Match(
        sent => Results.Ok(sent),
        notFound => Results.NotFound(),
        validationErrors => Results.ValidationProblem(ConvertValidationErrorsToValidationProblem(validationErrors))
    );
});
```

### Advanced Features

You may want to create custom [Fluid](https://github.com/sebastienros/fluid/) tags for use with your implementation. This can easily be done by customizing how the `FluidParser` is registered:

```csharp
builder.Services.AddMailService(fluidParser =>
{
    fluidParser.RegisterEmptyTag("dummy", async (System.IO.TextWriter writer, System.Text.Encodings.Web.TextEncoder encoder, Fluid.TemplateContext context) =>
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(context);

        await writer.WriteAsync("""<img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAIAAAAmkwkpAAAAQ0lEQVR4nGKanbxR0iCpx+Lsq7MfGBKt0pls/206J81Y9Yrx7rta3mMP0lrvGci3MjNw/wj989bz5ksPzqOAAAAA//94+BjST+Y61wAAAABJRU5ErkJggg==" alt="Logo">""").ConfigureAwait(false);
        return Fluid.Ast.Completion.Normal;
    });
});
```

One common scenario of a tag could be dynamic generation of inline images:

```csharp
builder.Services.AddMailService(fluidParser =>
{
    fluidParser.RegisterExpressionTag("logo", async (Fluid.Ast.Expression value, System.IO.TextWriter writer, System.Text.Encodings.Web.TextEncoder encoder, Fluid.TemplateContext context) =>
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(context);

        if (!context.AmbientValues.TryGetValue(Constants.Extensibility, out IFluidExtensibility? extensibility))
        {
            return Fluid.Ast.Completion.Normal;
        }

        Fluid.Values.FluidValue fluidValue = await value.EvaluateAsync(context).ConfigureAwait(false);
        if (fluidValue.ToStringValue() is not string colour)
        {
            return Fluid.Ast.Completion.Normal;
        }

        string cid = extensibility!.AddInlineAttachment("logo.svg", Encoding.UTF8.GetBytes($"""<svg style="background-color: {colour};"></svg>"""), "image/svg+xml");
        await writer.WriteAsync($"""<img src="cid:{cid}" alt="Logo">""").ConfigureAwait(false);
        return Fluid.Ast.Completion.Normal;
    });
}
```

The `IFluidExtensibility` implementation currently only enables you to add new inline attachments, but could be extended further.

## Contributing

Contributions are welcome! If you encounter any issues or have suggestions for improvements, please open an issue or submit a pull request on the [TempMaiSe GitHub repository](https://github.com/AlbusKavaliro/TempMaiSe).

## License

TempMaiSe is released under the [MIT License](https://opensource.org/licenses/MIT).
