using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;

using System.Diagnostics.Metrics;
using AlbusKavaliro.TempMaiSe.Mailer;

namespace AlbusKavaliro.TempMaiSe.Samples.Api;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry tracing and metrics in a <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing and metrics configuration to the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="appBuilder">The <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <summary>
    /// Adds OpenTelemetry tracing and metrics configuration to the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="appBuilder">The <see cref="WebApplicationBuilder"/> to configure.</param>
    public static void AddOpenTelemetry<TService>(this WebApplicationBuilder appBuilder)
    {
        ArgumentNullException.ThrowIfNull(appBuilder);

        // Build a resource configuration action to set service information.
        void configureResource(ResourceBuilder r) => r.AddService(
            serviceName: appBuilder.Configuration?.GetValue<string>("ServiceName") ?? "unknown",
            serviceVersion: typeof(TService).Assembly.GetName().Version?.ToString() ?? "unknown",
            serviceInstanceId: Environment.MachineName);

        // Configure OpenTelemetry tracing & metrics with auto-start using the
        // AddOpenTelemetry extension from OpenTelemetry.Extensions.Hosting.
        appBuilder.Services.AddOpenTelemetry()
            .ConfigureResource(configureResource)
            .WithTracing(builder =>
            {
                // Tracing

                // Ensure the TracerProvider subscribes to any custom ActivitySources.
                builder
                    .AddSource(MailingInstrumentation.ActivitySourceName)
                    .SetSampler(new AlwaysOnSampler())
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                // Use IConfiguration binding for AspNetCore instrumentation options.
                appBuilder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(appBuilder.Configuration.GetSection("AspNetCoreTraceInstrumentation"));

                builder.AddConsoleExporter();
            })
            .WithMetrics(builder =>
            {
                // Metrics

                // Ensure the MeterProvider subscribes to any custom Meters.
                builder
                    .AddMeter(MailingInstrumentation.MeterName)
                    //.SetExemplarFilter(new TraceBasedExemplarFilter())
                    //.AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                builder.AddView(instrument =>
                {
                    return instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>)
                        ? new Base2ExponentialBucketHistogramConfiguration()
                        : null;
                });

                builder.AddConsoleExporter();
            });

        // Clear default logging providers used by WebApplication host.
        appBuilder.Logging.ClearProviders();

        // Configure OpenTelemetry Logging.
        appBuilder.Logging.AddOpenTelemetry(options =>
        {
            // Note: See appsettings.json Logging:OpenTelemetry section for configuration.

            var resourceBuilder = ResourceBuilder.CreateDefault();
            configureResource(resourceBuilder);
            options.SetResourceBuilder(resourceBuilder);

            options.AddConsoleExporter();
        });
    }
}
