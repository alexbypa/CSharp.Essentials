using Serilog;
using Serilog.Events;

namespace CSharpEssentials.HttpHelper;

/// <summary>
/// Internal logging for HttpHelper configuration (uses Serilog static logger).
/// </summary>
internal static class HttpHelperLog {
    private static ILogger Logger => Log.ForContext("ApplicationName", "HttpHelper")
        .ForContext("Action", "HttpHelper");

    internal static void Write(LogEventLevel level, Exception? ex, string message, params object?[] args) {
        if (ex is null)
            Logger.Write(level, message, args);
        else
            Logger.Write(level, ex, message, args);
    }
}
