namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// Provides constants for the TempMaiSe.Mailer namespace.
/// </summary>
public sealed class Constants
{
    /// <summary>
    /// Represents the name of the Extensibility object in the TemplateContext.
    /// </summary>
    public const string Extensibility = nameof(Extensibility);

    /// <summary>
    /// Represents the name of the IServiceProvider object in the TemplateContext.
    /// </summary>
    public const string ServiceProvider = nameof(ServiceProvider);

    /// <summary>
    /// Represents the name of the property indicating if the TemplateContext is currently used to render HTML.
    /// </summary>
    public const string IsHtml = nameof(IsHtml);
}
