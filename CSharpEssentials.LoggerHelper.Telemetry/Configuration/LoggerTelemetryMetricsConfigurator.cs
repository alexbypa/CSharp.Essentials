using CSharpEssentials.LoggerHelper.Telemetry.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration;
public static class LoggerTelemetryMetricsConfigurator {
    public static void Configure(IServiceCollection services, LoggerTelemetryOptions options, IConfiguration configuration) {
        CustomMetrics.Initialize(configuration);

        if (options.MeterListenerIsEnabled)
            services.AddHostedService<LoggerTelemetryMeterListenerService>();

        services.AddOpenTelemetry()
            .WithMetrics(metrics => {
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
