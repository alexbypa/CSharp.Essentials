using Microsoft.Extensions.Configuration;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Email;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
namespace CSharpEssentials.LoggerHelper;


public interface IRequest {
    public string IdTransaction { get; }
    public string Action { get; }
}

public static class LoggerExtensionConfig {
    public static IServiceCollection addloggerConfiguration(this IServiceCollection services, IHostApplicationBuilder builder) {
        var externalConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.LoggerHelper.json");
        if (File.Exists(externalConfigPath)) {
            builder.Configuration.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);
        }
        return services;
    }
}
public class loggerExtension<T> where T : IRequest {
    //TODO: Inserire tutte le opzioni di serilog del controller per ricordarsi facilmente di tutte le funzionalità esposte
    //TODO: Riprendere le altre tipologie di estensione Enrich etc etc json ....
    public static readonly ILogger log = null;
    public static string postGreSQLConnectionString = "";
    static loggerExtension() {
        var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.LoggerHelper.json")
        .Build();

        var loggingConfigurationSection = configuration.GetSection("Serilog:SerilogConfiguration");
        var loggingConfig = loggingConfigurationSection.Get<SerilogConfiguration>();

        //TODO: da ottimizzare va in lock
        //Serilog.Debugging.SelfLog.Enable(msg => File.AppendAllText(Path.Combine(loggingConfig.SerilogOption.File.Path, "serilog-selflog.txt"), msg));

        //TODO: da parametrizzare
        var columnWriters = new Dictionary<string, ColumnWriterBase>{
            { "timestamp", new TimestampColumnWriter() },
            { "level", new LevelColumnWriter() },
            { "message", new RenderedMessageColumnWriter() },
            { "exception", new ExceptionColumnWriter() },
            { "properties", new LogEventSerializedColumnWriter() },
            { "IdTransaction", new SinglePropertyColumnWriter("IdTransaction", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
            { "MachineName", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
            { "Action", new SinglePropertyColumnWriter("Action", PropertyWriteMethod.ToString, NpgsqlDbType.Text) }
        };


        log = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Conditional(evt => {
                if (loggingConfig != null && loggingConfig.SerilogCondition.FirstOrDefault(level => level.Level != null && level.Sink.Equals("PostgreSQL") && level.Level.Contains(evt.Level.ToString())) != null)
                    return true;
                else
                    return false;
            },
            wt => {
                // Aggiungi PostgreSQL solo se la condizione è vera
                if (loggingConfig.SerilogCondition.Any(level =>
                        level.Sink.Equals("PostgreSQL") &&
                        level.Level != null)) {
                        wt.PostgreSQL(
                            connectionString: loggingConfig.SerilogOption.PostgreSQL.connectionstring,
                            tableName: "Logs",
                            columnOptions: columnWriters,
                            needAutoCreateTable: true,
                            failureCallback: e => {
                                string conn = configuration.GetConnectionString(loggingConfig.SerilogOption.PostgreSQL.connectionstring);
                                Console.WriteLine($"CONN : {conn} Errore durante il logging su PostgreSQL: {e.Message}");
                            }
                        );
                    }
                }
            )
            .WriteTo.Conditional(evt => {
                if (loggingConfig != null && loggingConfig.SerilogCondition.FirstOrDefault(level => level.Level != null && level.Sink.Equals("MSSqlServer") && level.Level.Contains(evt.Level.ToString())) != null)
                    return true;
                else
                    return false;
                }, wt => wt.MSSqlServer(loggingConfig.SerilogOption.MSSqlServer.connectionString,
                    new MSSqlServerSinkOptions {
                    TableName = loggingConfig.SerilogOption.MSSqlServer.sinkOptionsSection.tableName,
                    SchemaName = loggingConfig.SerilogOption.MSSqlServer.sinkOptionsSection.schemaName,
                    AutoCreateSqlTable = loggingConfig.SerilogOption.MSSqlServer.sinkOptionsSection.autoCreateSqlTable,
                    BatchPostingLimit = loggingConfig.SerilogOption.MSSqlServer.sinkOptionsSection.batchPostingLimit,
                    BatchPeriod = TimeSpan.Parse(loggingConfig.SerilogOption.MSSqlServer.sinkOptionsSection.period)
                }, columnOptions: GetColumnOptions()))
            .WriteTo.Conditional(evt => {
                return loggingConfig != null && loggingConfig.SerilogCondition.FirstOrDefault(item => item.Level != null && item.Sink.Equals("File") && item.Level.Contains(evt.Level.ToString())) != null;
            }, wt => wt.File("logs/log.txt"))
            .WriteTo.Conditional(evt => {
                if (loggingConfig != null && loggingConfig.SerilogCondition.FirstOrDefault(level => level.Level != null && level.Sink.Equals("ElasticSearch") && level.Level.Contains(evt.Level.ToString())) != null)
                    return true;
                else
                    return false;
            }, wt => wt.Elasticsearch())//TODO: da provare
            .WriteTo.Conditional(evt => {
                return loggingConfig != null && loggingConfig.SerilogCondition.FirstOrDefault(item => item.Level != null && item.Sink.Equals("Email") && item.Level.Contains(evt.Level.ToString())) != null;
            }, wt => wt.Email(new EmailSinkOptions { From = "alexbypa@gmail.com" })) //TODO:
            .WriteTo.Conditional(evt => {
                if (loggingConfig != null && loggingConfig.SerilogCondition.FirstOrDefault(item => item.Level != null && item.Sink.Equals("Telegram") && item.Level.Contains(evt.Level.ToString())) != null)
                    return true;
                else
                    return false;
            }, wt => wt.Telegram(loggingConfig.SerilogOption.TelegramOption.Api_Key, loggingConfig.SerilogOption.TelegramOption.chatId))
            .WriteTo.Console(LogEventLevel.Information)
            .CreateLogger();
    }
    public Dictionary<string, List<int>> loglevels { get; set; }

    /// <summary>
    /// method to write log
    /// </summary>
    /// <param name="Action">Action is the parameter that indicates the area of ​​interest in which the log is being written</param>
    /// <param name="IdTransaction">IdTransaction is the reference code for retrieving the log (for example, if you use a warehouse program, each operation on a product could be the barcode of the item)</param>
    /// <param name="level">level is the LogEventLevel of serilog</param>
    /// <param name="ex">is the text of the error to report</param>
    /// <param name="message">indicates the log message</param>
    /// <param name="args">are additional parameters that can help identify particular scenarios</param>
    public static async void TraceAsync(IRequest request, LogEventLevel level, Exception? ex, string message, params object[] args) {
        await Task.Run(() => TraceSync(request, level, ex, message, args));
    }

    /// <summary>
    /// method to write log
    /// </summary>
    /// <param name="Action">Action is the parameter that indicates the area of ​​interest in which the log is being written</param>
    /// <param name="IdTransaction">IdTransaction is the reference code for retrieving the log (for example, if you use a warehouse program, each operation on a product could be the barcode of the item)</param>
    /// <param name="level">level is the LogEventLevel of serilog</param>
    /// <param name="ex">is the text of the error to report</param>
    /// <param name="message">indicates the log message</param>
    /// <param name="args">are additional parameters that can help identify particular scenarios</param>
    public static void TraceSync(IRequest request, LogEventLevel level, Exception? ex, string message, params object[] args) {
        message += " {IdTransaction} {MachineName} {Action}";
        List<object> arguments = new List<object>();
        if (args != null)
            arguments = args.ToList();

        string IdTransaction = Guid.NewGuid().ToString();
        string ActionLog = "UNDEFINED";
        if (request == null) {
            IdTransaction = Guid.NewGuid().ToString();
            ActionLog = "CallFromExternalBusiness";
        } else {
            IdTransaction = request.IdTransaction;
            ActionLog = request.Action;
        }

        arguments.Add(IdTransaction);
        arguments.Add(Environment.MachineName);
        arguments.Add(ActionLog);

        Thread.Sleep(500);

        if (message.Split("{").Length - 1 != arguments.Count) {
            log.Error(new Exception("parametri non validi su loggerExtension"), message);
        }

        if (level == LogEventLevel.Debug)
            log.Debug(message, arguments.ToArray());
        if (level == LogEventLevel.Information)
            log.Information(message, arguments.ToArray());
        if (level == LogEventLevel.Warning)
            log.Warning(message, arguments.ToArray());
        if (level == LogEventLevel.Error)
            log.Error(ex, message, arguments.ToArray());
        if (level == LogEventLevel.Fatal)
            log.Fatal(ex, message, arguments.ToArray());

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
}
