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

            builder.Services
                .AddOptions<LoggerTelemetryOptions>()
                .Bind(builder.Configuration.GetSection("Serilog:SerilogConfiguration:LoggerTelemetryOptions"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var options = services.BuildServiceProvider()
                          .GetRequiredService<IOptions<LoggerTelemetryOptions>>()
                          .Value;

            bool canContinueWithTelemetry = true;
            LoggerTelemetryDbConfigurator.InitializeMigrationsAndDbContext(services, out canContinueWithTelemetry);
            if (canContinueWithTelemetry == false ) {
                Console.WriteLine("LoggerTelemetry is disabled. Skipping telemetry setup.");
                return services;
            }

            services.AddSingleton<ITelemetryGatekeeper, TelemetryGatekeeper>((sp) => {
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<LoggerTelemetryOptions>>();
                return new TelemetryGatekeeper(optionsMonitor);
            });



            services.AddSingleton<ILoggerTelemetryTraceEntryFactory, LoggerTelemetryTraceEntryFactory>(sp => {
                var gatekeeper = sp.GetRequiredService<ITelemetryGatekeeper>();
                return new LoggerTelemetryTraceEntryFactory(gatekeeper);
            });
            services.AddSingleton<ILoggerTelemetryTraceEntryRepository, LoggerTelemetryTraceEntryRepository>();

            // Registers a startup filter that ensures the TraceIdPropagationMiddleware is injected
            // at the beginning of the request pipeline, before any telemetry is collected.
            // This guarantees that all logs, metrics, and traces will carry the trace_id from the start.
            services.AddSingleton<IStartupFilter, TraceIdPropagationStartupFilter>();

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