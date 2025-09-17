using Fluid;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AlbusKavaliro.TempMaiSe.Mailer;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for adding the mail service to the <see cref="IServiceCollection"/>.
/// </summary>
public static class MailServiceExtensions
{
    /// <summary>
    /// Adds the mail service to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the mail service to.</param>
    /// <summary>
    /// Adds the mail service to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the mail service to.</param>
    /// <param name="configureParser">An optional action to configure the <see cref="FluFluidMailParseridParser"/>.</param>
    /// <param name="options">The <see cref="FluidParserOptions"/> to use.</param>
    public static void AddMailService(this IServiceCollection services, Action<IServiceProvider, FluidMailParser>? configureParser = null, FluidParserOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(serviceProvider =>
        {
            options ??= new();
            FluidMailParser parser = new(options);
            configureParser?.Invoke(serviceProvider, parser);
            return parser;
        });

        services.TryAddSingleton<FluidParser>(serviceProvider => serviceProvider.GetRequiredService<FluidMailParser>());

        services.TryAddSingleton<ITemplateToMailMapper, TemplateToMailMapper>();
        services.TryAddSingleton<IMailInformationToMailMapper, MailInformationToMailMapper>();

        services.TryAddSingleton<IDataParser, DataParser>();

        services.TryAddScoped<IMailService, MailService>();
    }
}
