using System.Runtime.CompilerServices;
using CSharpEssentials.LoggerHelper;
using Serilog;
using Serilog.Events;

namespace CSharpEssentials.HttpHelper;

/// <summary>
/// Internal logging for HttpHelper configuration: writes to the LoggerHelper pipeline when
/// <c>AddLoggerHelper</c> has run, otherwise to the Serilog static logger.
/// </summary>
internal static class HttpHelperLog {
    internal static ILogger Logger => (TryGetPipelineLogger() ?? Log.Logger)
        .ForContext("ApplicationName", "HttpHelper")
        .ForContext("Action", "HttpHelper");

    // Isolated so an incompatible LoggerHelper (older, or without the internal member) fails here
    // at JIT/access time and falls back to Log.Logger instead of throwing from an error path.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ILogger? TryGetPipelineLogger() {
        try {
            return LegacyLoggerHolder.Instance;
        } catch (Exception ex) when (ex is MemberAccessException or TypeLoadException) {
            return null;
        }
    }

    internal static void Write(LogEventLevel level, Exception? ex, string message, params object?[] args) {
        if (ex is null)
            Logger.Write(level, message, args);
        else
            Logger.Write(level, ex, message, args);
    }
}
