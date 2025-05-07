using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.Email;
using Serilog.Sinks.MSSqlServer;
using System.Data;
using System.Diagnostics;
using System.Net;

namespace CSharpEssentials.LoggerHelper;
public class LoggerBuilder {
    private readonly LoggerConfiguration _config;
    private readonly SerilogConfiguration _serilogConfig;
    private readonly IConfiguration _configuration;

    public LoggerBuilder(IConfiguration configuration) {
        _configuration = configuration;
        _serilogConfig = configuration.GetSection("Serilog:SerilogConfiguration").Get<SerilogConfiguration>();
        _config = new LoggerConfiguration().ReadFrom.Configuration(configuration);
        Serilog.Debugging.SelfLog.Enable(msg => File.AppendAllText(Path.Combine(_serilogConfig?.SerilogOption?.File?.Path, "serilog-selflog.txt"), msg)); //TODO: da ottimizzare
    }

    public LoggerBuilder AddDynamicSinks() {
        foreach (var condition in _serilogConfig.SerilogCondition ?? Enumerable.Empty<SerilogCondition>()) {
            Debug.Print(condition.Sink);
            if (condition.Level == null || !condition.Level.Any())
                continue;

            switch (condition.Sink) {
                case "File":
                    var path = _serilogConfig.SerilogOption?.File?.Path ?? "logs/log.txt";
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => wt.File(path));
                    break;
                case "Telegram": //TODO: non arrivano i messaggi 
                    _config.WriteTo.Conditional(
                        evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                        wt => {
                            var apiKey = _serilogConfig?.SerilogOption?.TelegramOption?.Api_Key;
                            var chatId = _serilogConfig?.SerilogOption?.TelegramOption?.chatId;
                            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(chatId))
                                wt.Telegram(apiKey, chatId);
                        });
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
                                        indexFormat: _serilogConfig?.SerilogOption?.ElasticSearch?.indexFormat);

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
                        wt => wt.Email(options: new EmailSinkOptions {
                            From = "xxxxxxx@gmail.com",
                            Port = 587,
                            Host = "xxx",
                            To = new List<string> { "alexbypa@gmail.com" },
                            Credentials = new NetworkCredential("xxxxxxxxx@gmail.com", "------")
                        })
                    );//TODO:To better understand how it works and use the html template
                    break;
            }
        }

        return this;
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

    public ILogger Build() => _config.WriteTo.Console().CreateLogger();


}
