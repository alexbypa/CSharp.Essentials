using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public static class LoggerExtensionFluent<T> where T : IRequest {
    public static async Task<ILogTraceContext<T>> TraceAsync(
            T request,
            LogEventLevel level,
            Exception? ex,
            string message,
            params object[] args) {
        loggerExtension<T>.TraceSync(request, level, ex, message, args); // static call
        return await Task.FromResult(new LogTraceContext<T>(request));
    }
}