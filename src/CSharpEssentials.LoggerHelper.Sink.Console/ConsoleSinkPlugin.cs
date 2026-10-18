using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Console;

// ── Options (owned by this sink package, not the core) ───────────

/// <summary>
/// Configuration options for the Console sink.
/// </summary>
public sealed class ConsoleSinkOptions {
    public string? OutputTemplate { get; set; }

    /// <summary>
    /// Minimum interval between two events passed to this sink route. Null (default) or zero disables throttling.
    /// JSON example: "00:00:20"
    /// </summary>
    public TimeSpan? ThrottleInterval { get; set; }
}

// ── Builder extension (OCP: core doesn't know about this sink) ───

public static class ConsoleBuilderExtensions {
    public static LoggerHelperBuilder ConfigureConsole(this LoggerHelperBuilder builder, Action<ConsoleSinkOptions> configure)
        => builder.ConfigureSink("Console", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

[LoggerHelperSink]
public sealed class ConsoleSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "Console", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<ConsoleSinkOptions>("Console")
                   ?? options.BindSinkSection<ConsoleSinkOptions>("Console");

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.Sink(new ColoredConsoleSink(opts?.OutputTemplate, new ThrottleGate("Console", opts?.ThrottleInterval)))
        );
    }
}

// ── Sink implementation ───────────────────────────────────────────

internal sealed class ColoredConsoleSink : ILogEventSink {
    private readonly MessageTemplateTextFormatter? _formatter;
    private readonly IThrottleGate _throttle;

    internal ColoredConsoleSink(string? outputTemplate, IThrottleGate throttle) {
        _throttle = throttle;
        _formatter = string.IsNullOrWhiteSpace(outputTemplate) ? null : new MessageTemplateTextFormatter(outputTemplate);
    }

    public void Emit(LogEvent logEvent) {
        if (!_throttle.TryPass(logEvent))
            return;

        string formatted;
        if (_formatter is null) {
            var message = logEvent.RenderMessage();
            var exception = logEvent.Exception?.ToString();
            formatted = $"[{logEvent.Timestamp.ToLocalTime():HH:mm:ss} {logEvent.Level}] {message}";

            if (!string.IsNullOrEmpty(exception))
                formatted += $" {exception}";
        } else {
            var sw = new StringWriter();
            _formatter.Format(logEvent, sw);
            formatted = sw.ToString().TrimEnd('\r', '\n');
        }

        System.Console.ForegroundColor = GetColor(logEvent.Level);
        try {
            System.Console.WriteLine(formatted);
        } finally {
            System.Console.ResetColor();
        }
    }

    private static ConsoleColor GetColor(LogEventLevel level) => level switch {
        LogEventLevel.Verbose => ConsoleColor.DarkGray,
        LogEventLevel.Debug => ConsoleColor.Gray,
        LogEventLevel.Information => ConsoleColor.Blue,
        LogEventLevel.Warning => ConsoleColor.DarkYellow,
        LogEventLevel.Error => ConsoleColor.Red,
        LogEventLevel.Fatal => ConsoleColor.DarkRed,
        _ => ConsoleColor.White,
    };
}

// ── Auto-registration ─────────────────────────────────────────────

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new ConsoleSinkPlugin());
}
