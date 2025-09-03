using Newtonsoft.Json.Schema;

using System.Diagnostics;

using AlbusKavaliro.TempMaiSe.Mailer;
using AlbusKavaliro.TempMaiSe.Models;
using AlbusKavaliro.TempMaiSe.Samples.Api;

using OneOf;
using OneOf.Types;
using System.Text;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager config = builder.Configuration;

builder.AddOpenTelemetry<Program>();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddFluentEmail(config);

builder.Services.AddMailService((_, fluidParser) =>
{
    fluidParser.RegisterEmptyTag("dummy", async (TextWriter writer, System.Text.Encodings.Web.TextEncoder encoder, Fluid.TemplateContext context) =>
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(context);

        await writer.WriteAsync("""<img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAIAAAAmkwkpAAAAQ0lEQVR4nGKanbxR0iCpx+Lsq7MfGBKt0pls/206J81Y9Yrx7rta3mMP0lrvGci3MjNw/wj989bz5ksPzqOAAAAA//94+BjST+Y61wAAAABJRU5ErkJggg==" alt="Dummy">""").ConfigureAwait(false);
        return Fluid.Ast.Completion.Normal;
    });

    fluidParser.RegisterExpressionTag("logo", async (Fluid.Ast.Expression value, TextWriter writer, System.Text.Encodings.Web.TextEncoder encoder, Fluid.TemplateContext context) =>
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
});

builder.Services.AddTemplateContext(config);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IPartialRepository, PartialRepository>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TemplateContext>();

builder.Services.AddRazorPages();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

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

app.Run();

public partial class Program { }
