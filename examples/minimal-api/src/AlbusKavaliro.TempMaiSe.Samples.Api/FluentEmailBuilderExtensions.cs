using FluentEmail.MailKitSmtp;

namespace AlbusKavaliro.TempMaiSe.Samples.Api;

public static class FluentEmailBuilderExtensions
{
    private const string Smtp = nameof(Smtp);
    private const string MailKit = nameof(MailKit);

    private static readonly HashSet<string> s_supportedSenders = [Smtp, MailKit];

    public static FluentEmailServicesBuilder AddFluentEmail(this IServiceCollection services, ConfigurationManager config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        IConfigurationSection fluentEmailConfigSection = config.GetRequiredSection(nameof(FluentEmail));
        return services.AddFluentEmail(
            fluentEmailConfigSection.GetValue("FromAddress", "dummy@example.org"),
            fluentEmailConfigSection.GetValue("FromName", ""))
            .AddSenderFromConfiguration(fluentEmailConfigSection);
    }

    private static FluentEmailServicesBuilder AddSenderFromConfiguration(this FluentEmailServicesBuilder builder, IConfigurationSection fluentEmailConfigSection)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fluentEmailConfigSection);

        string? sender = fluentEmailConfigSection.GetValue("Sender", "None");
        if (sender == null || !s_supportedSenders.Contains(sender))
        {
            throw new InvalidOperationException($"Sender '{sender}' is not valid.");
        }

        switch (sender)
        {
            case Smtp:
                IConfigurationSection smtpConfigSection = fluentEmailConfigSection.GetRequiredSection(Smtp);
                string host = smtpConfigSection.GetValue("Server", "example.org")!;
                int port = smtpConfigSection.GetValue("Port", 21)!;
                builder = builder.AddSmtpSender(host, port);
                break;
            case MailKit:
                SmtpClientOptions mailKitOptions = fluentEmailConfigSection.GetRequiredSection(MailKit).Get<SmtpClientOptions>()
                    ?? throw new InvalidOperationException($"Sender '{sender}' requires a valid config section.");
                builder = builder.AddMailKitSender(mailKitOptions);
                break;
            default:
                // Just don't configure a sender. Maybe log a warning about this?
                break;
        }

        return builder;
    }
}
