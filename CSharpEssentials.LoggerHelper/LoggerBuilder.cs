using Microsoft.Extensions.Configuration;
using NpgsqlTypes;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Email;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using System.Data;
using System.Diagnostics;
using System.Net;

namespace CSharpEssentials.LoggerHelper;
public class LoggerBuilder {
    private readonly LoggerConfiguration _config;
    private readonly SerilogConfiguration _serilogConfig;
    public LoggerBuilder(IConfiguration configuration) {
        var appName = configuration["Serilog:SerilogConfiguration:ApplicationName"];
        _serilogConfig = configuration.GetSection("Serilog:SerilogConfiguration").Get<SerilogConfiguration>();
        _config = new LoggerConfiguration().ReadFrom.Configuration(configuration)
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
    public LoggerBuilder AddDynamicSinks() {
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
                                new TelegramMarkdownFormatter()));
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
                                    columnOptions: new Dictionary<string, ColumnWriterBase>
                                    {
                                        { "ApplicationName", new SinglePropertyColumnWriter("ApplicationName", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
                                        {"message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
                                        {"message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
                                        {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
                                        {"raise_date", new TimestampColumnWriter(NpgsqlDbType.Timestamp) },
                                        {"exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
                                        {"properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
                                        {"props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
                                        {"MachineName", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l") },
                                        {"Action", new SinglePropertyColumnWriter("Action", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
                                        {"IdTransaction", new SinglePropertyColumnWriter("IdTransaction", PropertyWriteMethod.ToString, NpgsqlDbType.Text) }
                                 }
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
                        }, columnOptions: GetColumnOptions()
                        ));
                    break;
                case "ElasticSearch"://TODO: non sono ancora riuscito a trovare i logs su elasticsearch
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
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
                        //TODO: to hack if setting is null
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => wt.Email(
                            options: new EmailSinkOptions {
                                From = _serilogConfig?.SerilogOption?.Email.From,
                                Port = (int)_serilogConfig.SerilogOption.Email.Port,
                                Host = _serilogConfig.SerilogOption.Email.Host,
                                To = _serilogConfig.SerilogOption?.Email.To.ToList(),
                                Credentials = new NetworkCredential(_serilogConfig?.SerilogOption?.Email.CredentialHost, _serilogConfig?.SerilogOption?.Email.CredentialPassword),
                                IsBodyHtml = true
                            })
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
    public static ColumnWriterBase CreateColumnWriter(string typeName, List<string> args) {
        var type = Type.GetType(typeName, throwOnError: true);
        var ctor = type
            .GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Length == args.Count);

        if (ctor == null)
            throw new InvalidOperationException($"No constructor with {args.Count} args found for {typeName}");

        var parameters = ctor.GetParameters();
        var typedArgs = args
            .Select((arg, i) => ConvertArgument(arg, parameters[i].ParameterType))
            .ToArray();

        return (ColumnWriterBase)ctor.Invoke(typedArgs);
    }

    private static object ConvertArgument(string value, Type targetType) {
        if (targetType.IsEnum)
            return Enum.Parse(targetType, value);
        return Convert.ChangeType(value, targetType);
    }

    public static Serilog.Sinks.MSSqlServer.ColumnOptions GetColumnOptions() {
        var columnOptions = new Serilog.Sinks.MSSqlServer.ColumnOptions();
        // Override the default Primary Column of Serilog by custom column name
        //columnOptions.Id.ColumnName = "LogId";

        // Removing all the default column
        columnOptions.Store.Add(StandardColumn.LogEvent);
        //columnOptions.Store.Remove(StandardColumn.MessageTemplate);
        //columnOptions.Store.Remove(StandardColumn.Properties);

        // Adding all the custom columns
        columnOptions.AdditionalColumns = new List<SqlColumn> {
            new SqlColumn { DataType = SqlDbType.VarChar, ColumnName = "IdTransaction", DataLength = 250, AllowNull = false },
            new SqlColumn { DataType = SqlDbType.VarChar, ColumnName = "MachineName", DataLength = 250, AllowNull = false },
            new SqlColumn { DataType = SqlDbType.VarChar, ColumnName = "Action", DataLength = 250, AllowNull = false }
        };
        return columnOptions;
    }
    //public ILogger Build() => _config.WriteTo.Console().CreateLogger();
    public ILogger Build() => _config.CreateLogger();

}
