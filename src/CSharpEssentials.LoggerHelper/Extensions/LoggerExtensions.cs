using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Lightweight extensions for ILogger that add IdTransaction/Action/SpanName enrichment.
///
/// Usage (scope-based — set once per operation, all logs inherit):
///
///   using (logger.BeginTrace("OrderProcess", "TXN-001")) {
///       logger.LogInformation("Order {OrderId} placed", orderId);
///       logger.LogError(ex, "Payment failed for {OrderId}", orderId);
///   }
///
/// For fire-and-forget single-shot logging (backward compat with original TraceAsync):
///
///   logger.Trace("OrderProcess", "TXN-001", LogLevel.Error, ex, "Failed {OrderId}", orderId);
/// </summary>
public static class LoggerExtensions {
    /// <summary>
    /// Creates a logging scope enriched with IdTransaction, Action, and SpanName (if active).
    /// All logs within the scope automatically include these properties.
    ///
    /// This is the recommended API — set the scope once per operation, then use
    /// standard ILogger methods (LogInformation, LogError, etc.) inside.
    ///
    /// The scope cost is paid once, not per-log-call.
    /// </summary>
    public static IDisposable? BeginTrace(this ILogger logger, string action, string idTransaction) {
        var spanName = Activity.Current?.DisplayName;

        // Use KeyValuePair array instead of Dictionary — avoids Dict internal allocations
        // (buckets array, entries array, hash computation). A KVP array is the cheapest
        // IEnumerable<KVP> that BeginScope recognizes.
        KeyValuePair<string, object?>[] state = spanName is not null
            ? [
                new("IdTransaction", idTransaction),
                new("Action", action),
                new("SpanName", spanName)
              ]
            : [
                new("IdTransaction", idTransaction),
                new("Action", action)
              ];

        return logger.BeginScope(state);
    }

    /// <summary>
    /// Logs a single message enriched with IdTransaction and Action by appending them
    /// to the message template. No scope/AsyncLocal overhead — properties are inlined.
    ///
    /// Use this for isolated one-off logs where a scope would be overkill.
    /// For multiple logs in the same operation, prefer BeginTrace + standard ILogger.
    /// </summary>
    public static void Trace(this ILogger logger, string action, string idTransaction,
        LogLevel level, Exception? exception, string message, params object?[] args) {
        if (!logger.IsEnabled(level))
            return;

        bool hasSpan = Activity.Current is not null;
        int extraCount = hasSpan ? 3 : 2;

        var enrichedArgs = new object?[args.Length + extraCount];
        args.CopyTo(enrichedArgs, 0);
        enrichedArgs[args.Length] = idTransaction;
        enrichedArgs[args.Length + 1] = action;

        string enrichedMessage;
        if (hasSpan) {
            enrichedArgs[args.Length + 2] = Activity.Current!.DisplayName;
            enrichedMessage = string.Concat(message, " {IdTransaction} {Action} {SpanName}");
        } else {
            enrichedMessage = string.Concat(message, " {IdTransaction} {Action}");
        }

        logger.Log(level, exception, enrichedMessage, enrichedArgs);
    }

    /// <summary>
    /// Trace shorthand — Information level, no exception.
    /// </summary>
    public static void Trace(this ILogger logger, string action, string idTransaction,
        string message, params object?[] args) {
        Trace(logger, action, idTransaction, LogLevel.Information, null, message, args);
    }
}
