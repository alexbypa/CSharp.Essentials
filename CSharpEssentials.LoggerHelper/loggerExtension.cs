using CSharpEssentials.LoggerHelper.model;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;


#if NET6_0
using Microsoft.AspNetCore.Builder;
#endif

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Interface representing the basic logging request data.
/// </summary>
public interface ILoggerRequest {
    /// <summary>
    /// Gets the unique transaction ID for tracking logs.
    /// </summary>
    public string IdTransaction { get; }
    /// <summary>
    /// Gets the action name or context for the log entry.
    /// </summary>
    public string Action { get; }
    /// <summary>
    /// Gets the application name associated with the log entry.
    /// </summary>
    public string ApplicationName { get; }
}
/// <summary>
/// Marker interface extending ILoggerRequest for request-based logging.
/// </summary>
public interface IRequest : ILoggerRequest { }

// 1) Define a TextWriter that pushes into your queue
class ErrorListTextWriter : TextWriter {
    private readonly List<LogErrorEntry> _errors;

    public ErrorListTextWriter(List<LogErrorEntry> errors) {
        _errors = errors;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value) {
        if (string.IsNullOrEmpty(value))
            return;

        _errors.Add(new LogErrorEntry {
            Timestamp = DateTime.UtcNow,
            SinkName = "SelfLog",
            ErrorMessage = value,
            ContextInfo = AppContext.BaseDirectory
        });
    }
    // You can optionally override Write(char) if you need to
}
/// <summary>
/// Static logger extension for writing log entries enriched with transaction context.
/// </summary>
/// <typeparam name="T">The request type implementing IRequest.</typeparam>
public class loggerExtension<T> where T : IRequest {
    protected static readonly ILogger log;
    public static string CurrentError { get; set; }
    public static readonly List<LogErrorEntry> Errors = new();
    public static List<string> SinksLoaded = new List<string>();
    static loggerExtension() {
        string step = "Init";
        string SinkNameInError = "";
        try {

            Serilog.Debugging.SelfLog.Enable(
                new ErrorListTextWriter(Errors)
            );

            //Serilog.Debugging.SelfLog.Enable(msg =>
            //{
            //    Errors.Add(new LogErrorEntry {
            //        Timestamp = DateTime.UtcNow,
            //        SinkName = "SelfLog",
            //        ErrorMessage = msg,
            //        ContextInfo = AppContext.BaseDirectory
            //    });
            //});

            var builder = new LoggerBuilder().AddDynamicSinks(out step, out SinkNameInError, ref Errors, ref SinksLoaded);
            log = builder.Build();
            var enricher = LoggerHelperServiceLocator.GetService<IContextLogEnricher>();
            log = enricher != null
                   ? enricher.Enrich(log, context: null)
                   : log;
            
        } catch (Exception ex) {
            if (string.IsNullOrEmpty(CurrentError))
                CurrentError = $"{step} [{AppContext.BaseDirectory}]: {ex.Message}";
            var entry = new LogErrorEntry {
                Timestamp = DateTime.UtcNow,
                SinkName = SinkNameInError,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                ContextInfo = AppContext.BaseDirectory
            };
            Errors.Add(entry);
        }
    }
    /// <summary>
    /// method to write log Async
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
    /// method to write log Sync
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

        var IdTransaction = request?.IdTransaction ?? Guid.NewGuid().ToString();
        var Action = request?.Action ?? "UNKNOWN";
        arguments.Add(IdTransaction);
        arguments.Add(Environment.MachineName);
        arguments.Add(Action);

        var spanName = Activity.Current?.DisplayName;
        if (log == null)
            return;

        var logger = log;
        if (!string.IsNullOrEmpty(spanName))
            logger = log.ForContext("SpanName", spanName);

        int totPlaceHolders = arguments.Count;
        try {
            var matches = Regex.Matches(message, @"(?<!\{)\{[a-zA-Z_][a-zA-Z0-9_]*\}(?!\})", RegexOptions.None, TimeSpan.FromMilliseconds(200));
            totPlaceHolders = matches.Count;
            if (totPlaceHolders != arguments.Count) {
                logger.Warning("LoggerHelper: Placeholder count mismatch. MessageTemplate: {Message}, Expected: {Expected}, Actual: {Actual}",
                message, matches.Count, arguments.Count);
            }
        } catch (Exception exRegEx) {
            logger.Warning("LoggerHelper: Regex failed to validate placeholders: {Error}", exRegEx.Message);
            Errors.Add(new LogErrorEntry {
                Timestamp = DateTime.UtcNow,
                SinkName = "TraceSync",
                ErrorMessage = $"Regex validation failed: {exRegEx.Message}",
                StackTrace = exRegEx.ToString(),
                ContextInfo = request?.Action ?? "n/a"
            });
        }
        var enricher = LoggerHelperServiceLocator.GetService<IContextLogEnricher>();
        if (enricher != null)
            logger = enricher.Enrich(logger, request);

        logger.Write(level, ex, message, arguments.ToArray());
    }
}
