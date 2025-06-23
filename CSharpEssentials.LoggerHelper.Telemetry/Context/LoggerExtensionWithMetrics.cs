using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Telemetry.Context;
public static class LoggerExtensionWithMetrics<T> where T : IRequest {
    public static ILogTraceContext<T> TraceAsync(
            T request,
            LogEventLevel level,
            Exception? ex,
            string message,
            params object[] args) {
        Task.Run(() => loggerExtension<T>.TraceSync(request, level, ex, message, args));

        return new LogTraceContext<T>(request);
    }
}