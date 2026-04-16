using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Console;

// ── Options (owned by this sink package, not the core) ───────────

/// <summary>
/// Configuration options for the Console sink.
/// </summary>
public sealed class ConsoleSinkOptions {
    public string? OutputTemplate { get; set; }
}

// ── Builder extension (OCP: core doesn't know about this sink) ───

public static class ConsoleBuilderExtensions {
    public static LoggerHelperBuilder ConfigureConsole(this LoggerHelperBuilder builder, Action<ConsoleSinkOptions> configure)
        => builder.ConfigureSink("Console", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

internal sealed class ConsoleSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "Console", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<ConsoleSinkOptions>("Console")
                   ?? options.BindSinkSection<ConsoleSinkOptions>("Console");

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.Sink(new ColoredConsoleSink(opts?.OutputTemplate))
        );
    }
}

// ── Sink implementation ───────────────────────────────────────────

internal sealed class ColoredConsoleSink : ILogEventSink {
    private readonly string? _template;

    internal ColoredConsoleSink(string? outputTemplate) {
        _template = outputTemplate;
    }

    public void Emit(LogEvent logEvent) {
        System.Console.ForegroundColor = GetColor(logEvent.Level);

        var message = logEvent.RenderMessage();
        var exception = logEvent.Exception?.ToString();
        var formatted = $"[{logEvent.Timestamp.ToLocalTime():HH:mm:ss} {logEvent.Level}] {message}";

        if (!string.IsNullOrEmpty(exception))
            formatted += $" {exception}";

        System.Console.WriteLine(formatted);
        System.Console.ResetColor();
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
