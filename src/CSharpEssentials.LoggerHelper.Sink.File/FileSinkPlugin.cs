using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CSharpEssentials.LoggerHelper.Sink.File;

// ── Options ───────────────────────────────────────────────────────

public sealed class FileSinkOptions {
    public string Path { get; set; } = "Logs";
    public string RollingInterval { get; set; } = "Day";
    public int RetainedFileCountLimit { get; set; } = 7;
    public bool Shared { get; set; } = true;

    /// <summary>
    /// When set, log events are routed to subdirectories named after the value
    /// of this log event property. Events without the property are written to
    /// the base path (same as standard behavior).
    /// Example: FileNameProperty = "TenantId" → Logs/acme/log-.txt
    /// </summary>
    public string? FileNameProperty { get; set; }

    /// <summary>
    /// Maximum number of property-based sub-sinks kept open simultaneously.
    /// When exceeded, the least recently used sink is disposed. Default: 64.
    /// </summary>
    public int MaxOpenFiles { get; set; } = 64;
}

// ── Builder extension ─────────────────────────────────────────────

public static class FileBuilderExtensions {
    public static LoggerHelperBuilder ConfigureFile(this LoggerHelperBuilder builder, Action<FileSinkOptions> configure)
        => builder.ConfigureSink("File", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

[LoggerHelperSink]
public sealed class FileSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "File", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<FileSinkOptions>("File")
                   ?? options.BindSinkSection<FileSinkOptions>("File")
                   ?? new FileSinkOptions();

        if (string.IsNullOrWhiteSpace(opts.FileNameProperty)) {
            var logDirectory = opts.Path;
            var logFilePath = System.IO.Path.Combine(logDirectory, "log-.txt");
            Directory.CreateDirectory(logDirectory);

            loggerConfig.WriteTo.Conditional(
                evt => routing.Matches(evt.Level),
                wt => wt.File(
                    new JsonFormatter(),
                    logFilePath,
                    rollingInterval: Enum.Parse<RollingInterval>(opts.RollingInterval),
                    retainedFileCountLimit: opts.RetainedFileCountLimit,
                    shared: opts.Shared
                )
            );
        } else {
            var dynamicSink = new DynamicPropertyFileSink(opts);
            loggerConfig.WriteTo.Conditional(
                evt => routing.Matches(evt.Level),
                wt => wt.Sink(dynamicSink)
            );
        }
    }
}

// ── Dynamic property-based file sink ──────────────────────────────

internal sealed class DynamicPropertyFileSink : ILogEventSink, IDisposable {
    private readonly FileSinkOptions _opts;
    private readonly RollingInterval _rollingInterval;
    private readonly JsonFormatter _formatter = new();
    private readonly Lazy<ILogEventSink> _defaultSink;
    private readonly ConcurrentDictionary<string, SinkEntry> _sinks = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _evictionLock = new();
    private bool _disposed;

    internal DynamicPropertyFileSink(FileSinkOptions opts) {
        _opts = opts;
        _rollingInterval = Enum.Parse<RollingInterval>(opts.RollingInterval);
        _defaultSink = new Lazy<ILogEventSink>(() => CreateSink(opts.Path));
    }

    public void Emit(LogEvent logEvent) {
        if (_disposed) return;

        var sink = ResolveSink(logEvent);
        sink.Emit(logEvent);
    }

    private ILogEventSink ResolveSink(LogEvent logEvent) {
        if (!logEvent.Properties.TryGetValue(_opts.FileNameProperty!, out var propValue))
            return _defaultSink.Value;

        var raw = propValue is ScalarValue sv ? sv.Value?.ToString() : propValue.ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return _defaultSink.Value;

        var key = SanitizeFileName(raw);

        var entry = _sinks.GetOrAdd(key, k => {
            var dir = System.IO.Path.Combine(_opts.Path, k);
            return new SinkEntry(CreateSink(dir));
        });

        entry.Touch();
        EvictIfNeeded();
        return entry.Sink;
    }

    private ILogEventSink CreateSink(string directory) {
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, "log-.txt");

        var config = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(
                _formatter,
                path,
                rollingInterval: _rollingInterval,
                retainedFileCountLimit: _opts.RetainedFileCountLimit,
                shared: _opts.Shared
            );

        return config.CreateLogger();
    }

    private void EvictIfNeeded() {
        if (_sinks.Count <= _opts.MaxOpenFiles) return;

        lock (_evictionLock) {
            if (_sinks.Count <= _opts.MaxOpenFiles) return;

            var toEvict = _sinks
                .OrderBy(kv => kv.Value.LastUsed)
                .Take(_sinks.Count - _opts.MaxOpenFiles)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in toEvict) {
                if (_sinks.TryRemove(key, out var entry))
                    (entry.Sink as IDisposable)?.Dispose();
            }
        }
    }

    // Compiled once at class load: SanitizeFileName is called on every log event
    // when FileNameProperty is configured — the interpreted static Regex.Replace overload
    // hits an internal LRU cache (capacity ~15) and risks re-compilation under load.
    private static readonly Regex _unsafeChars =
        new(@"[\\/:*?""<>|]", RegexOptions.Compiled, matchTimeout: TimeSpan.FromSeconds(1));

    private static string SanitizeFileName(string value) {
        var sanitized = _unsafeChars.Replace(value, "_");
        if (sanitized.Length > 100)
            sanitized = sanitized[..100];
        return sanitized.Trim().TrimEnd('.');
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;

        foreach (var entry in _sinks.Values)
            (entry.Sink as IDisposable)?.Dispose();
        _sinks.Clear();

        if (_defaultSink.IsValueCreated)
            (_defaultSink.Value as IDisposable)?.Dispose();
    }

    private sealed class SinkEntry {
        public ILogEventSink Sink { get; }
        public DateTime LastUsed { get; private set; }

        public SinkEntry(ILogEventSink sink) {
            Sink = sink;
            LastUsed = DateTime.UtcNow;
        }

        public void Touch() => LastUsed = DateTime.UtcNow;
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new FileSinkPlugin());
}
