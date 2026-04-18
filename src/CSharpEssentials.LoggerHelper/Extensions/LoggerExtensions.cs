using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Extension methods that bridge the original TraceSync/TraceAsync pattern
/// to standard ILogger.
///
/// Enrichment is done by appending {IdTransaction} {Action} to the message template
/// (same approach as the original loggerExtension&lt;T&gt;). This avoids BeginScope/AsyncLocal
/// overhead which is extremely expensive in hot paths.
///
/// TraceSync  — logs synchronously on the calling thread
/// TraceAsync — fire-and-forget: offloads the log to a background thread via Task.Run
/// </summary>
public static class LoggerExtensions {
    private const string Suffix = " {IdTransaction} {Action}";
    private const string SuffixWithSpan = " {IdTransaction} {Action} {SpanName}";

    /// <summary>
    /// Logs a message synchronously, enriched with IdTransaction, Action, and SpanName.
    /// Equivalent to the original loggerExtension&lt;T&gt;.TraceSync.
    /// </summary>
    public static void TraceSync(this ILogger logger, string action, string idTransaction,
        LogLevel level, Exception? exception, string message, params object?[] args) {
        if (!logger.IsEnabled(level))
            return;

        var spanName = Activity.Current?.DisplayName;
        WriteEnriched(logger, level, exception, action, idTransaction, spanName, message, args);
    }

    /// <summary>
    /// TraceSync shorthand — Information level, no exception.
    /// </summary>
    public static void TraceSync(this ILogger logger, string action, string idTransaction,
        string message, params object?[] args) {
        TraceSync(logger, action, idTransaction, LogLevel.Information, null, message, args);
    }

    /// <summary>
    /// Logs a message asynchronously (fire-and-forget), enriched with IdTransaction, Action, and SpanName.
    /// Equivalent to the original loggerExtension&lt;T&gt;.TraceAsync.
    ///
    /// The call returns immediately — the log is written on a background thread.
    /// </summary>
    public static void TraceAsync(this ILogger logger, string action, string idTransaction,
        LogLevel level, Exception? exception, string message, params object?[] args) {
        if (!logger.IsEnabled(level))
            return;

        // Capture SpanName on the calling thread (Activity.Current is thread-local)
        var spanName = Activity.Current?.DisplayName;

        Task.Run(() => WriteEnriched(logger, level, exception, action, idTransaction, spanName, message, args));
    }

    /// <summary>
    /// TraceAsync shorthand — Information level, no exception.
    /// </summary>
    public static void TraceAsync(this ILogger logger, string action, string idTransaction,
        string message, params object?[] args) {
        TraceAsync(logger, action, idTransaction, LogLevel.Information, null, message, args);
    }

    private static void WriteEnriched(ILogger logger, LogLevel level, Exception? exception,
        string action, string idTransaction, string? spanName, string message, object?[] args) {
        bool hasSpan = spanName is not null;
        int extraCount = hasSpan ? 3 : 2;

        // Allocate exact-size array directly — for 4-5 elements, this is faster
        // than ArrayPool (Rent always returns oversized → forces a copy anyway)
        var enrichedArgs = new object?[args.Length + extraCount];
        Array.Copy(args, enrichedArgs, args.Length);
        enrichedArgs[args.Length] = idTransaction;
        enrichedArgs[args.Length + 1] = action;
        if (hasSpan)
            enrichedArgs[args.Length + 2] = spanName;

        var enrichedMessage = string.Concat(message, hasSpan ? SuffixWithSpan : Suffix);

        logger.Log(level, exception, enrichedMessage, enrichedArgs);
    }
}
