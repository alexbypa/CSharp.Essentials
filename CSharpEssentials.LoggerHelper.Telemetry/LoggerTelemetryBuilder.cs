using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CSharpEssentials.LoggerHelper.Telemetry {
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
            // Load telemetry configuration from JSON file (debug or production)
            var configuration = new ConfigurationBuilder()
#if DEBUG
    .AddJsonFile("appsettings.LoggerHelper.debug.json")
#else
    .AddJsonFile("appsettings.LoggerHelper.json")
#endif
        .Build();
            LoggerTelemetryOptions loggerTelemetryOptions = configuration.GetSection("Serilog:SerilogConfiguration:LoggerTelemetryOptions").Get<LoggerTelemetryOptions>();
            if (!loggerTelemetryOptions?.IsEnabled ?? true)
                return services;

            services.AddDbContext<TelemetriesDbContext>(options =>
                options.UseNpgsql(loggerTelemetryOptions.ConnectionString));
            
            // Applica automaticamente le migration se mancanti
            using (var scope = services.BuildServiceProvider().CreateScope()) {
                var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();
                db.Database.Migrate();
            }
            // Initialize any custom metrics (e.g., static meters)
            CustomMetrics.Initialize(builder.Configuration);
            builder.Services.AddHostedService<OpenTelemetryMeterListenerService>();

            services.AddControllers();
            services.AddOpenTelemetry()
                
                .WithMetrics(metricProvider => {
                    metricProvider
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddView(instrumentName: "*", new ExplicitBucketHistogramConfiguration {
                            TagKeys = new[] { "trace_id" }
                        })
                        .AddReader(new PeriodicExportingMetricReader(new PostgreSqlMetricExporter(services.BuildServiceProvider()), 20000, 30000))//TODO: Settare gli intervalli !
                        .AddMeter("LoggerHelper.Metrics") //Commento in quanto deve filtrare tutto
                        .AddConsoleExporter();
                })
                
                .WithTracing(tracerProviderBuilder => {
                    tracerProviderBuilder
                        //.AddSource("LoggerHelper")
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("LoggerHelper"))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        //.AddProcessor(new SimpleActivityExportProcessor(new PostgreSqlTraceExporter(services.BuildServiceProvider())))
                        .AddProcessor(new BatchActivityExportProcessor(
                            new PostgreSqlTraceExporter(services.BuildServiceProvider()),
                            maxQueueSize: 2048,
                            scheduledDelayMilliseconds: 5000,
                            exporterTimeoutMilliseconds: 30000
                        ))
                        .AddConsoleExporter(); // Per vedere le trace anche su console
                });

            return services;
        }
    }
}
