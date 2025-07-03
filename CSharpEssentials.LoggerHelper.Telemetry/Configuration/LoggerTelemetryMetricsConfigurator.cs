using CSharpEssentials.LoggerHelper.Telemetry.Metrics;
using CSharpEssentials.LoggerHelper.Telemetry.Proxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration;
public static class LoggerTelemetryMetricsConfigurator {
    public static void Configure(IServiceCollection services, LoggerTelemetryOptions options, IConfiguration configuration) {
        var provider = services.BuildServiceProvider();
        var telemetryGatekeeper = provider.GetRequiredService<ITelemetryGatekeeper>();
        CustomMetrics.Initialize(configuration);

        if (options.MeterListenerIsEnabled)
            services.AddHostedService<LoggerTelemetryMeterListenerService>();

        services.AddOpenTelemetry()
            .WithMetrics(metrics => {
                if (telemetryGatekeeper.IsEnabled)
                    metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("LoggerHelper.Metrics")
                    .AddView("LoggerHelper.Metrics.*", new ExplicitBucketHistogramConfiguration {
                        TagKeys = new[] { "trace_id" }
                    })
                    .AddConsoleExporter();
            });
    }
}
