using System.Diagnostics;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Non-generic holder for the shared Serilog logger instance.
/// Set once during AddLoggerHelper() startup, read by all loggerExtension&lt;T&gt; instantiations.
/// </summary>
internal static class LegacyLoggerHolder {
    internal static volatile Serilog.ILogger? Instance;
}

/// <summary>
/// Backward-compatible static logger that mirrors the original loggerExtension&lt;T&gt; API.
/// Delegates to the Serilog ILogger configured by AddLoggerHelper().
///
/// New code should use ILogger&lt;T&gt; with BeginTrace/Trace extensions instead:
///
///   // Old API:
///   loggerExtension&lt;MyRequest&gt;.TraceAsync(request, LogEventLevel.Information, null, "msg");
///
///   // New API:
///   using (logger.BeginTrace("action", "txnId")) {
///       logger.LogInformation("msg");
///   }
/// </summary>
/// <typeparam name="T">The request type implementing IRequest.</typeparam>
[Obsolete("Use ILogger<T> with BeginTrace/Trace extensions instead. See LoggerExtensions class.")]
public class loggerExtension<T> where T : IRequest {

    /// <summary>
    /// Logs a message asynchronously, enriched with IdTransaction, MachineName, and Action.
    /// </summary>
    [Obsolete("Use ILogger<T> with BeginTrace scope instead.")]
    public static async void TraceAsync(IRequest request, LogEventLevel level, Exception? ex, string message, params object[] args) {
        await Task.Run(() => TraceSync(request, level, ex, message, args));
    }

    /// <summary>
    /// Logs a message synchronously, enriched with IdTransaction, MachineName, and Action.
    /// </summary>
    [Obsolete("Use ILogger<T> with BeginTrace scope instead.")]
    public static void TraceSync(IRequest request, LogEventLevel level, Exception? ex, string message, params object[] args) {
        var log = LegacyLoggerHolder.Instance;
        if (log is null)
            return;

        message += " {IdTransaction} {MachineName} {Action}";

        var arguments = new List<object>();
        if (args is not null)
            arguments.AddRange(args);

        arguments.Add(request?.IdTransaction ?? Guid.NewGuid().ToString());
        arguments.Add(Environment.MachineName);
        arguments.Add(request?.Action ?? "UNKNOWN");

        var logger = log;
        var spanName = Activity.Current?.DisplayName;
        if (!string.IsNullOrEmpty(spanName))
            logger = logger.ForContext("SpanName", spanName);

        logger.Write(level, ex, message, arguments.ToArray());
    }

    /// <summary>
    /// Logs a message asynchronously with a "Dashboard" target sink marker.
    /// </summary>
    [Obsolete("Use ILogger<T> with BeginTrace scope instead.")]
    public static async void TraceDashBoardAsync(IRequest request, LogEventLevel level, Exception? ex, string message, params object[] args) {
        var newArgs = args.ToList();
        newArgs.Add("Dashboard");
        await Task.Run(() => TraceSync(request, level, ex, message + "{TargetSink}", newArgs.ToArray()));
    }

    /// <summary>
    /// Logs a message synchronously with a "Dashboard" target sink marker.
    /// </summary>
    [Obsolete("Use ILogger<T> with BeginTrace scope instead.")]
    public static void TraceDashBoardSync(IRequest request, LogEventLevel level, Exception? ex, string message, params object[] args) {
        var newArgs = args.ToList();
        newArgs.Add("Dashboard");
        TraceSync(request, level, ex, message + "{TargetSink}", newArgs.ToArray());
    }
}
