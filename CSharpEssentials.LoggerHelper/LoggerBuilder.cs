using CSharpEssentials.LoggerHelper.CustomSinks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;
using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Responsible for building the Serilog logger configuration dynamically based on the provided appsettings.json configuration.
/// </summary>
internal class LoggerBuilder {
private readonly LoggerConfiguration _config;
    private readonly SerilogConfiguration _serilogConfig;
    IConfiguration _configuration;
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerBuilder"/> class.
    /// Reads the Serilog configuration section and sets up basic enrichers and self-logging.
    /// </summary>
    /// <param name="configuration">Application configuration (e.g., appsettings.json).</param>
    internal LoggerBuilder(IConfiguration configuration) {
        _configuration = configuration;

        var appName = configuration["Serilog:SerilogConfiguration:ApplicationName"];
        _serilogConfig = configuration.GetSection("Serilog:SerilogConfiguration").Get<SerilogConfiguration>();
        _config = new LoggerConfiguration().ReadFrom.Configuration(configuration)
            .WriteTo.Sink(new OpenTelemetryLogEventSink())//TODO: da configurare
            .Enrich.WithProperty("ApplicationName", appName)
            .Enrich.With<RenderedMessageEnricher>();
        var selfLogPath = Path.Combine(_serilogConfig?.SerilogOption?.File?.Path, "serilog-selflog.txt");
        var stream = new FileStream(
            selfLogPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite
        );

        var writer = new StreamWriter(stream) { AutoFlush = true };
        Serilog.Debugging.SelfLog.Enable(msg => {
            writer.WriteLine(msg);
        });

    }
    /// <summary>
    /// Dynamically adds sinks to the LoggerConfiguration based on conditions specified in the Serilog configuration.
    /// </summary>
    /// <returns>The current instance of LoggerBuilder for chaining.</returns>
    internal LoggerBuilder AddDynamicSinks() {
        foreach (var condition in _serilogConfig.SerilogCondition ?? Enumerable.Empty<SerilogCondition>()) {
            Debug.Print(condition.Sink);
            if (condition.Level == null || !condition.Level.Any())
                continue;

            switch (condition.Sink) {
                case "File":
                    var logDirectory = _serilogConfig?.SerilogOption?.File?.Path ?? "Logs";
                    var logFilePath = Path.Combine(logDirectory, "log-.txt");
                    Directory.CreateDirectory(logDirectory);
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => wt.File(
                            new JsonFormatter(),
                            logFilePath,
                            rollingInterval: Enum.Parse<RollingInterval>(_serilogConfig?.SerilogOption?.File?.RollingInterval ?? "Day"),
                            retainedFileCountLimit: _serilogConfig?.SerilogOption?.File?.RetainedFileCountLimit ?? 7,
                            shared: _serilogConfig?.SerilogOption?.File?.Shared ?? true
                            )
                        );
                    break;
                case "Telegram":
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => {
                            wt.Sink(new CustomTelegramSink(
                                _serilogConfig?.SerilogOption?.TelegramOption?.Api_Key,
                                _serilogConfig?.SerilogOption?.TelegramOption?.chatId,
                                new CustomTelegramSinkFormatter()));
                        });
                    break;
                case "PostgreSQL":
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => {
                            wt.PostgreSQL(
                                    connectionString: _serilogConfig?.SerilogOption?.PostgreSQL?.connectionstring,
                                    tableName: _serilogConfig.SerilogOption.PostgreSQL.tableName,
                                    schemaName: _serilogConfig.SerilogOption.PostgreSQL.schemaName,
                                    needAutoCreateTable: true,
                                    columnOptions: CustomPostgresQLSink.BuildPostgresColumns(_serilogConfig).GetAwaiter().GetResult()
                                );
                        }
                        );
                    break;
                case "MSSqlServer":
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => wt.MSSqlServer(_serilogConfig?.SerilogOption?.MSSqlServer?.connectionString,
                        new MSSqlServerSinkOptions {
                            TableName = _serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.tableName,
                            SchemaName = _serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.schemaName,
                            AutoCreateSqlTable = _serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.autoCreateSqlTable ?? false,
                            BatchPostingLimit = _serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.batchPostingLimit ?? 100,
                            BatchPeriod = string.IsNullOrEmpty(_serilogConfig?.SerilogOption?.MSSqlServer?.sinkOptionsSection?.period) ? TimeSpan.FromSeconds(10) : TimeSpan.Parse(_serilogConfig.SerilogOption.MSSqlServer.sinkOptionsSection.period),
                        }, 
                        //columnOptions: CustomMSSQLServerSink.GetColumnOptions()
                        columnOptions: CustomMSSQLServerSink.GetColumnsOptions_v2(_serilogConfig?.SerilogOption.MSSqlServer)
                        ));
                    break;
                case "ElasticSearch"://TODO: non sono ancora riuscito a trovare i logs su elasticsearch
                    _config.WriteTo.Conditional(
                        evt =>  _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => {
                            var elasticUrl = _serilogConfig?.SerilogOption?.ElasticSearch?.nodeUris ?? "http://localhost:9200";
                            try {
                                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                                var response = client.GetAsync(elasticUrl).Result;
                                if (response.IsSuccessStatusCode) {
                                    wt.Elasticsearch(
                                        nodeUris: _serilogConfig?.SerilogOption?.ElasticSearch?.nodeUris,
                                        indexFormat: _serilogConfig?.SerilogOption?.ElasticSearch?.indexFormat,
                                        autoRegisterTemplate: true,
                                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
                                        );

                                } else {
                                    Serilog.Debugging.SelfLog.WriteLine($"[ElasticSearch] HTTP status non valido: {response.StatusCode}");
                                }
                            } catch (Exception ex) {
                                Serilog.Debugging.SelfLog.WriteLine($"[ElasticSearch] Non raggiungibile: {ex.Message}");
                            }
                        });
                    break;
                case "Email":
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => wt.Sink(new CustomEmailSink(
                            smtpServer: _serilogConfig.SerilogOption?.Email.Host,
                            smtpPort: (int)_serilogConfig.SerilogOption?.Email.Port,
                            fromEmail: _serilogConfig.SerilogOption?.Email.From,
                            toEmail: string.Join(",", _serilogConfig.SerilogOption?.Email.To),
                            username: _serilogConfig.SerilogOption?.Email.username,
                            password: _serilogConfig.SerilogOption?.Email.password,
                            subjectPrefix: "[LoggerHelper]",
                            enableSsl: (bool)_serilogConfig.SerilogOption?.Email?.EnableSsl,
                            templatePath: _serilogConfig.SerilogOption?.Email?.TemplatePath 
                        ))
                    );
                    break;
                case "Console":
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => wt.Console()
                    );
                    break;
            }
        }

        return this;
    }
    /// <summary>
    /// Builds and returns the configured Serilog logger instance.
    /// </summary>
    /// <returns>The created <see cref="ILogger"/> instance.</returns>
    public ILogger Build() => _config.CreateLogger();
}
