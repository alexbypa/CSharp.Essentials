using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public static class LoggerExtension<T> where T : IRequest {
    public static ILogTraceContext<T> TraceAsync(
            T request,
            LogEventLevel level,
            Exception? ex,
            string message,
            params object[] args) {
        Task.Run(() => loggerExtensionCore<T>.TraceSync(request, level, ex, message, args));

        return new LogTraceContext<T>(request);
    }
}