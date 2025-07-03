using CSharpEssentials.LoggerHelper.Configuration;
using CSharpEssentials.LoggerHelper.Telemetry.Exporters;
using CSharpEssentials.LoggerHelper.Telemetry.Metrics;
using CSharpEssentials.LoggerHelper.Telemetry.middlewares;
using CSharpEssentials.LoggerHelper.Telemetry.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            

            services.AddSingleton<ITelemetryGatekeeper, TelemetryGatekeeper>((sp) => {
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<LoggerTelemetryOptions>>();
                var LoggerConfigInfo = sp.GetRequiredService<ILoggerConfigInfo>();
                return new TelemetryGatekeeper(optionsMonitor, LoggerConfigInfo);
            });
            LoggerTelemetryDbConfigurator.Configure(services, options);

            services.AddSingleton<ILoggerTelemetryTraceEntryFactory, LoggerTelemetryTraceEntryFactory>(sp => {
                var gatekeeper = sp.GetRequiredService<ITelemetryGatekeeper>();
                return new LoggerTelemetryTraceEntryFactory(gatekeeper);
            });
            services.AddSingleton<ILoggerTelemetryTraceEntryRepository, LoggerTelemetryTraceEntryRepository>();

            // Registers a startup filter that ensures the TraceIdPropagationMiddleware is injected
            // at the beginning of the request pipeline, before any telemetry is collected.
            // This guarantees that all logs, metrics, and traces will carry the trace_id from the start.
            services.AddSingleton<IStartupFilter, TraceIdPropagationStartupFilter>();

            //TODO:
            // Dovremmo aggiungere queste metriche custom su metric listener o no !
            // Initialize any custom metrics (e.g., static meters)
            CustomMetrics.Initialize(builder.Configuration);


            // Esportazione metriche e traces tramite HostedService LoggerTelemetryMeterListenerService
            if (options?.MeterListenerIsEnabled ?? false) {
                builder.Services.AddSingleton<IMetricEntryFactory, MetricEntryFactory>();
                builder.Services.AddSingleton<IMetricEntryRepository, MetricEntryRepository>();
                builder.Services.AddHostedService<LoggerTelemetryMeterListenerService>();
            }

            services.AddControllers();

            LoggerTelemetryMetricsConfigurator.Configure(services, options, builder.Configuration);
            LoggerTelemetryTracingConfigurator.Configure(services);

            return services;
        }
    }
}