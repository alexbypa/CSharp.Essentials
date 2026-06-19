using Hangfire.Console;
using Hangfire.Server;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.Sink.HangfireConsole;

// ── Options (owned by this sink package, not the core) ───────────

/// <summary>
/// Configuration options for the HangfireConsole sink.
/// </summary>
public sealed class HangfireConsoleSinkOptions {
    /// <summary>
    /// Optional format provider for rendering log messages.
    /// </summary>
    public IFormatProvider? FormatProvider { get; set; }
}

// ── PerformContext accessor (same pattern as IHttpContextAccessor) ──

/// <summary>
/// Permette di accedere al PerformContext di Hangfire da qualsiasi punto del codice.
/// Il contesto è disponibile solo durante l'esecuzione di un job Hangfire.
/// Fuori da un job, Current restituisce null.
/// </summary>
public interface IPerformContextAccessor {
    PerformContext? Current { get; }
    void Set(PerformContext context);
    void Clear();
}

/// <summary>
/// Implementazione thread-safe di IPerformContextAccessor tramite AsyncLocal.
/// AsyncLocal mantiene il valore per il flusso async corrente (e i suoi figli),
/// senza interferire con altri job in esecuzione parallela.
/// </summary>
public sealed class PerformContextAccessor : IPerformContextAccessor {
    private static readonly AsyncLocal<PerformContext?> _current = new();

    public PerformContext? Current => _current.Value;
    public void Set(PerformContext context) => _current.Value = context;
    public void Clear() => _current.Value = null;
}

// ── Builder extension (OCP: core doesn't know about this sink) ───

public static class HangfireConsoleBuilderExtensions {
    public static LoggerHelperBuilder ConfigureHangfireConsole(this LoggerHelperBuilder builder, Action<HangfireConsoleSinkOptions> configure)
        => builder.ConfigureSink("HangfireConsole", configure);
}

// ── DI extensions ────────────────────────────────────────────────

public static class HangfireConsoleServiceCollectionExtensions {
    /// <summary>
    /// Registra IPerformContextAccessor come singleton nel DI container.
    /// Chiamare questo metodo in fase di startup per abilitare il sink HangfireConsole.
    /// </summary>
    public static IServiceCollection AddHangfireConsoleSink(this IServiceCollection services) {
        var accessor = new PerformContextAccessor();
        services.AddSingleton<IPerformContextAccessor>(accessor);
        HangfireConsoleSinkAccessorHolder.Accessor = accessor;
        return services;
    }
}

// ── Plugin ────────────────────────────────────────────────────────

[LoggerHelperSink]
public sealed class HangfireConsoleSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "HangfireConsole", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<HangfireConsoleSinkOptions>("HangfireConsole")
                   ?? options.BindSinkSection<HangfireConsoleSinkOptions>("HangfireConsole");

        // IPerformContextAccessor viene risolto dal DI in fase di startup.
        // Il sink deve essere creato con l'accessor per poter accedere al PerformContext.
        var accessor = HangfireConsoleSinkAccessorHolder.Accessor;
        if (accessor is null) {
            Serilog.Debugging.SelfLog.WriteLine("HangfireConsole sink configured in routes but IPerformContextAccessor not registered. Call services.AddHangfireConsoleSink() at startup.");
            return;
        }

        loggerConfig.WriteTo.Conditional(
            evt => routing.ShouldEmit(evt.Level),
            wt => wt.Sink(new HangfireConsoleSerilogSink(accessor, opts?.FormatProvider))
        );
    }
}

// ── Static holder for the accessor (set during DI registration) ──

/// <summary>
/// Holds a static reference to the IPerformContextAccessor registered in DI.
/// This is needed because sink plugins are resolved before the DI container is built.
/// </summary>
public static class HangfireConsoleSinkAccessorHolder {
    internal static IPerformContextAccessor? Accessor { get; set; }
}

// ── Sink implementation ───────────────────────────────────────────

/// <summary>
/// Serilog Sink che scrive i log sulla dashboard Hangfire tramite PerformContext.
/// Se non siamo dentro un job Hangfire (PerformContext è null), il log viene ignorato.
/// Supporta colori diversi in base al LogEventLevel.
/// </summary>
internal sealed class HangfireConsoleSerilogSink : ILogEventSink {
    private readonly IPerformContextAccessor _accessor;
    private readonly IFormatProvider? _formatProvider;

    internal HangfireConsoleSerilogSink(IPerformContextAccessor accessor, IFormatProvider? formatProvider = null) {
        _accessor = accessor;
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent) {
        var context = _accessor.Current;
        if (context is null)
            return;

        var message = logEvent.RenderMessage(_formatProvider);

        var color = logEvent.Level switch {
            LogEventLevel.Verbose => ConsoleTextColor.DarkGray,
            LogEventLevel.Debug => ConsoleTextColor.Gray,
            LogEventLevel.Information => ConsoleTextColor.White,
            LogEventLevel.Warning => ConsoleTextColor.Yellow,
            LogEventLevel.Error => ConsoleTextColor.Red,
            LogEventLevel.Fatal => ConsoleTextColor.DarkRed,
            _ => ConsoleTextColor.White
        };

        context.SetTextColor(color);
        context.WriteLine($"[{logEvent.Timestamp.ToLocalTime():HH:mm:ss} {logEvent.Level}] {message}");
        context.ResetTextColor();
    }
}

// ── Auto-registration ─────────────────────────────────────────────

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new HangfireConsoleSinkPlugin());
}
