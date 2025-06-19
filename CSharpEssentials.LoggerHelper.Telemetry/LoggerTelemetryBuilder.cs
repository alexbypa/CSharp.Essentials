using CSharpEssentials.LoggerHelper.Telemetry.Custom;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Services;
using CSharpEssentials.LoggerHelper.Telemetry.middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
            //TODO:
            services.AddDbContext<TelemetriesDbContext>(options =>
                options.UseNpgsql(loggerTelemetryOptions.ConnectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

            //TODO:
            // Applica automaticamente le migration se mancanti
            using (var scope = services.BuildServiceProvider().CreateScope()) {
                var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();
                db.Database.Migrate();
            }

            if (!loggerTelemetryOptions?.IsEnabled ?? true)
                return services;

            services.AddSingleton<IStartupFilter, TraceIdPropagationStartupFilter>();

            //TODO:
            // Dovremmo aggiungere queste metriche custom su metric listener o no !
            // Initialize any custom metrics (e.g., static meters)
            CustomMetrics.Initialize(builder.Configuration);

            if (loggerTelemetryOptions?.MeterListenerIsEnabled ?? false)
                builder.Services.AddHostedService<OpenTelemetryMeterListenerService>();

            services.AddControllers();

            services.AddOpenTelemetry()
                .WithMetrics(metricProvider => {
                    metricProvider
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSqlClientInstrumentation()
                        //TODO:
                        /*
                        .AddView(
                            instrumentName: "http.client.request.duration",
                            new ExplicitBucketHistogramConfiguration {
                                Boundaries = new double[] { 50, 200, 500, 1000 },
                                Name = "Alex",
                                Description = "Tempi di durata!",
                                TagKeys = new[] { "http.status_code", "trace_id" }
                            }
                        )
                        */
                        .AddRuntimeInstrumentation()

                        //TODO:come lo intercetto ? 
                        //.AddMeter("OpenTelemetry.Instrumentation.SqlClient") 

                        .AddMeter("LoggerHelper.Metrics")

                        .AddView(instrumentName: "LoggerHelper.Metrics.*", new ExplicitBucketHistogramConfiguration {
                            TagKeys = new[] { "trace_id" }
                        })

                        //TODO:
                        //.AddView("db.client.commands.duration", MetricStreamConfiguration.Drop)

                        .AddReader(new PeriodicExportingMetricReader(
                            new PostgreSqlMetricExporter(
                                services.BuildServiceProvider()),
                                loggerTelemetryOptions?.CustomExporter?.exportIntervalMilliseconds ?? 20000,
                                loggerTelemetryOptions?.CustomExporter?.exportTimeoutMilliseconds ?? 30000
                            )
                        )

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
                        ));
                    //TODO:
                    //.AddConsoleExporter(); // Per vedere le trace anche su console
                });
            return services;
        }
    }
}
