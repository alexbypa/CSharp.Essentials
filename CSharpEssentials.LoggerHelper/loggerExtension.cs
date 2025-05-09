using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Data;
using System.Text.RegularExpressions;

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
    //TODO: Riprendere le altre tipologie di estensione Enrich etc etc json ....
    public static readonly ILogger log;
    public static string postGreSQLConnectionString = "";
    static loggerExtension() {
        var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.LoggerHelper.json")
        .Build();

        var builder = new LoggerBuilder(configuration).AddDynamicSinks();
        log = builder.Build();
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

        int totPlaceHolders = arguments.Count;
        try {
            var matches = Regex.Matches(message, @"(?<!\{)\{[a-zA-Z_][a-zA-Z0-9_]*\}(?!\})", RegexOptions.None, TimeSpan.FromMilliseconds(200));
            totPlaceHolders = matches.Count;
            if (totPlaceHolders != arguments.Count) {
                log.Error(new Exception("parametri non validi su loggerExtension"), message);
            }
        } catch (Exception exRegEx) {
            log.Error(new Exception("parametri non validi su loggerExtension: " + exRegEx.Message), message);
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
