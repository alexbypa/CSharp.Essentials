using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Microsoft.Extensions.Logging provider that bridges ILogger&lt;T&gt; to LoggerHelper's
/// Serilog pipeline with per-level sink routing.
///
/// This allows any code using ILogger&lt;T&gt; to automatically route through LoggerHelper.
/// </summary>
[ProviderAlias("LoggerHelper")]
public sealed class LoggerHelperProvider : ILoggerProvider {
    private readonly Serilog.ILogger _serilogLogger;

    internal LoggerHelperProvider(Serilog.ILogger serilogLogger) {
        _serilogLogger = serilogLogger;
    }

    public ILogger CreateLogger(string categoryName) =>
        new LoggerHelperLogger(_serilogLogger.ForContext("SourceContext", categoryName));

    public void Dispose() {
        if (_serilogLogger is IDisposable disposable)
            disposable.Dispose();
    }
}
