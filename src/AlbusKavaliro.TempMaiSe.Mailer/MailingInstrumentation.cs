using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// It is recommended to use a custom type to hold references for
/// ActivitySource and Instruments. This avoids possible type collisions
/// with other components in the DI container.
/// </summary>
public sealed class MailingInstrumentation : IDisposable
{
    public const string ActivitySourceName = "TempMaiSe";

    public const string MeterName = "TempMaiSe";

    private readonly Meter _meter;

    private MailingInstrumentation()
    {
        string? version = typeof(MailingInstrumentation).Assembly.GetName().Version?.ToString();
        ActivitySource = new ActivitySource(ActivitySourceName, version);
        _meter = new Meter(MeterName, version);
        MailsSent = _meter.CreateCounter<long>("mail.sent.count", "E-Mails sent");
    }

    internal static MailingInstrumentation Instance { get; } = new();

    internal ActivitySource ActivitySource { get; }

    internal Counter<long> MailsSent { get; }

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }
}
