using CSharpEssentials.LoggerHelper.Telemetry.Exporters;
using CSharpEssentials.LoggerHelper.Telemetry.Proxy;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration;
public static class LoggerTelemetryTracingConfigurator {
    public static void Configure(IServiceCollection services) {
        var provider = services.BuildServiceProvider();
        var telemetryGatekeeper = provider.GetRequiredService<ITelemetryGatekeeper>();
        if (!telemetryGatekeeper.IsEnabled) {
            return;
        }
        var factory = provider.GetRequiredService<ILoggerTelemetryTraceEntryFactory>();
        var repository = provider.GetRequiredService<ILoggerTelemetryTraceEntryRepository>();
        services.AddOpenTelemetry()
            .WithTracing(tracer => {
                tracer
                    .AddSource("LoggerHelper")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("LoggerHelper"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddProcessor(new BatchActivityExportProcessor(
                        new LoggerTelemetryPostgreSqlTraceExporter(factory, repository),
                        maxQueueSize: 2048,
                        scheduledDelayMilliseconds: 5000,
                        exporterTimeoutMilliseconds: 30000
                    ));
            });
    }
}
