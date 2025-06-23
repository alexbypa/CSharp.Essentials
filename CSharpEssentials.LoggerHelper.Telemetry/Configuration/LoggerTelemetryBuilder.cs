using CSharpEssentials.LoggerHelper.Telemetry.Metrics;
using CSharpEssentials.LoggerHelper.Telemetry.middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration {
    /// <summary>
    /// Extension methods to configure OpenTelemetry tracing and metrics
    /// for the LoggerHelper package, including DB context, custom metrics,
    /// and exporters for PostgreSQL and console.
    /// </summary>
    public static class LoggerTelemetryBuilder {
        /// <summary>
        /// Adds and configures all telemetry services (metrics, tracing, DB) based
        /// on LoggerTelemetryOptions in configuration. Skips setup if disabled.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <param name="builder">The WebApplicationBuilder containing app configuration.</param>
        /// <returns>The modified IServiceCollection.</returns>
        public static IServiceCollection AddLoggerTelemetry(this IServiceCollection services, WebApplicationBuilder builder) {
            var options = TelemetryOptionsProvider.Load(builder);
            TelemetryDbConfigurator.Configure(services, options);

            if (!options?.IsEnabled ?? true)
                return services;

            services.AddSingleton<ITraceEntryFactory, TraceEntryFactory>();
            services.AddSingleton<ITraceEntryRepository, TraceEntryRepository>();

            // Registers a startup filter that ensures the TraceIdPropagationMiddleware is injected
            // at the beginning of the request pipeline, before any telemetry is collected.
            // This guarantees that all logs, metrics, and traces will carry the trace_id from the start.
            services.AddSingleton<IStartupFilter, TraceIdPropagationStartupFilter>();

            //TODO:
            // Dovremmo aggiungere queste metriche custom su metric listener o no !
            // Initialize any custom metrics (e.g., static meters)
            CustomMetrics.Initialize(builder.Configuration);


            //✔ Esportazione metriche e traces tramite HostedService OpenTelemetryMeterListenerService
            if (options?.MeterListenerIsEnabled ?? false) {
                builder.Services.AddSingleton<IMetricEntryFactory, MetricEntryFactory>();
                builder.Services.AddSingleton<IMetricEntryRepository, MetricEntryRepository>();
                builder.Services.AddHostedService<OpenTelemetryMeterListenerService>();
            }

            services.AddControllers();

            TelemetryMetricsConfigurator.Configure(services, options, builder);
            TelemetryTracingConfigurator.Configure(services);


            return services;
        }
    }
}
