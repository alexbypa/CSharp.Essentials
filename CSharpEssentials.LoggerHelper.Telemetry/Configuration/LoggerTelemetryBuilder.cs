using CSharpEssentials.LoggerHelper.Telemetry.Exporters;
using CSharpEssentials.LoggerHelper.Telemetry.Metrics;
using CSharpEssentials.LoggerHelper.Telemetry.middlewares;
using CSharpEssentials.LoggerHelper.Telemetry.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration {
    /// <summary>
    /// Extension methods to configure OpenTelemetry tracing and metrics
    /// for the LoggerHelper package, including DB context, custom metrics,
    /// and exporters for PostgreSQL and console.
    /// </summary>
    public static class LoggerTelemetryBuilder {

        static void DumpConfiguration(IEnumerable<IConfigurationSection> sections, string path) {
            foreach (var section in sections) {
                string currentKey = string.IsNullOrEmpty(path) ? section.Key : path + ":" + section.Key;
                string value = section.Value ?? "";

                // Se la sezione ha un valore (non è solo un contenitore)...
                if (!string.IsNullOrEmpty(section.Value)) {
                    // Stampa la chiave completa e il valore finale risolto.
                    // Se questo valore proviene da K8s, qui vedrai il valore di K8s.
                    Console.WriteLine($"[{currentKey}] = {value} | Path : {section.Path}");
                }
                // Se la sezione ha dei figli, scendi ricorsivamente
                DumpConfiguration(section.GetChildren(), currentKey);
            }
        }

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

            foreach(var config in builder.Configuration.GetChildren()) {
                Console.WriteLine($"Config Key: {config.Key}, Value: {config.Value} path: {config.Path}");
                
            }

            var configRoot = (IConfigurationRoot)builder.Configuration;
            DumpConfiguration(configRoot.GetChildren(), "");


            int providerIndex = 0;
            Console.WriteLine("--- CONFIGURATION SOURCES (Highest Priority Last) ---");
            foreach (var provider in configRoot.Providers) {
                Console.WriteLine($"[Source {providerIndex++}: {provider.GetType().Name}]");
                if (provider is Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider) {
                    Console.WriteLine($"Path : {((Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider)provider).Source.Path}");
                    
                }


            }



            var options = services.BuildServiceProvider()
                          .GetRequiredService<IOptions<LoggerTelemetryOptions>>()
                          .Value;
            if (options.IsEnabled == false) 
                return services;

            bool canContinueWithTelemetry = true;
            LoggerTelemetryDbConfigurator.InitializeMigrationsAndDbContext(services, builder.Configuration, out canContinueWithTelemetry);
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