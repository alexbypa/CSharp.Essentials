using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry;

namespace CSharpEssentials.LoggerHelper.Telemetry {
    public static class LoggerTelemetryBuilder {
        public static IServiceCollection AddLoggerTelemetry(this IServiceCollection services) {
            services.AddOpenTelemetry()
                .WithMetrics(metricProvider => {
                    metricProvider
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddView(instrumentName:"*", new ExplicitBucketHistogramConfiguration {
                            TagKeys = new[] { "trace_id" }
                        })
                        .AddReader(new PeriodicExportingMetricReader(new PostgreSqlMetricExporter(
                            services.BuildServiceProvider()
                        )))//TODO: Settare gli intervalli !
                        .AddMeter("LoggerHelper.Metrics")
                        .AddConsoleExporter(); 
                })
                .WithTracing(tracerProviderBuilder => {
                    tracerProviderBuilder
                        .AddSource("LoggerHelper")
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("LoggerHelper"))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddProcessor(new SimpleActivityExportProcessor(new PostgreSqlTraceExporter(services.BuildServiceProvider())))
                        .AddConsoleExporter(); // Per vedere le trace anche su console
                });

            return services;
        }
    }
}
