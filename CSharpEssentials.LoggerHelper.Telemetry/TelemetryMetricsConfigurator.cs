using CSharpEssentials.LoggerHelper.Telemetry.Custom;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public static class TelemetryMetricsConfigurator {
    public static void Configure(IServiceCollection services, LoggerTelemetryOptions options, WebApplicationBuilder builder) {
        CustomMetrics.Initialize(builder.Configuration);

        if (options.MeterListenerIsEnabled)
            services.AddHostedService<OpenTelemetryMeterListenerService>();

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
