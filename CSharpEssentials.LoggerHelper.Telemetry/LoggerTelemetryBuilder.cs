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
    public static class LoggerTelemetryBuilder {
        public static IServiceCollection AddLoggerTelemetry(this IServiceCollection services, WebApplicationBuilder builder) {
        var configuration = new ConfigurationBuilder()
#if DEBUG
    .AddJsonFile("appsettings.LoggerHelper.debug.json")
#else
    .AddJsonFile("appsettings.LoggerHelper.json")
#endif
        .Build();
            LoggerTelemetryOptions loggerTelemetryOptions = configuration.GetSection("Serilog:SerilogConfiguration:LoggerTelemetryOptions").Get<LoggerTelemetryOptions>();
            if (!loggerTelemetryOptions?.IsEnabled ?? false)
                return services;
            
            services.AddDbContext<TelemetriesDbContext>(options =>
                options.UseNpgsql(loggerTelemetryOptions.ConnectionString));

            CustomMetrics.Initialize(builder.Configuration);
            builder.Services.AddHostedService<OpenTelemetryMeterListenerService>();

            services.AddControllers();

            services.AddOpenTelemetry()
                .WithMetrics(metricProvider => {
                    metricProvider
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddView(instrumentName:"*", new ExplicitBucketHistogramConfiguration {
                            TagKeys = new[] { "trace_id" }
                        })
                        .AddReader(new PeriodicExportingMetricReader(new PostgreSqlMetricExporter(services.BuildServiceProvider()), 20000, 30000))//TODO: Settare gli intervalli !
                        //.AddMeter("LoggerHelper.Metrics") //Deve filtrare tutto
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
