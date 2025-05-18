using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry {
    public static class LoggerTelemetryBuilder {
        public static IServiceCollection AddLoggerTelemetry(this IServiceCollection services) {
            services.AddOpenTelemetry()
                .WithMetrics(metricProvider => {
                    metricProvider
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(otlp => {
                            otlp.Endpoint = new Uri("http://localhost:5133/v1/metrics"); // Porta della tua WebAPI ricevente
                            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                        })
                        .AddMeter("LoggerHelper.Metrics") 
                        .AddConsoleExporter(); 
                })
                .WithTracing(tracerProviderBuilder => {
                    tracerProviderBuilder
                        .AddSource("LoggerHelper")
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("LoggerHelper"))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(otlp =>
                        {
                            otlp.Endpoint = new Uri("http://localhost:5133/v1/traces"); // Porta della tua WebAPI ricevente
                            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                        })
                        .AddConsoleExporter(); // Per vedere le trace anche su console
                });

            return services;
        }
    }
}
