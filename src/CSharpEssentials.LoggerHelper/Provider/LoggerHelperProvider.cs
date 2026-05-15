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
    private readonly IContextLogEnricher? _contextEnricher;

    internal LoggerHelperProvider(Serilog.ILogger serilogLogger, IContextLogEnricher? contextEnricher = null) {
        _serilogLogger = serilogLogger;
        _contextEnricher = contextEnricher;
    }

    public ILogger CreateLogger(string categoryName) {
        var serilog = _serilogLogger.ForContext("SourceContext", categoryName);
        if (_contextEnricher is not null)
            serilog = _contextEnricher.Enrich(serilog, null);
        return new LoggerHelperLogger(serilog);
    }

    public void Dispose() {
        if (_serilogLogger is IDisposable disposable)
            disposable.Dispose();
    }
}
