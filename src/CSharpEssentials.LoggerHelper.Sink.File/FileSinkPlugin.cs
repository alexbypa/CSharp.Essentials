using Serilog;
using Serilog.Formatting.Json;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.File;

// ── Options ───────────────────────────────────────────────────────

public sealed class FileSinkOptions {
    public string Path { get; set; } = "Logs";
    public string RollingInterval { get; set; } = "Day";
    public int RetainedFileCountLimit { get; set; } = 7;
    public bool Shared { get; set; } = true;
}

// ── Builder extension ─────────────────────────────────────────────

public static class FileBuilderExtensions {
    public static LoggerHelperBuilder ConfigureFile(this LoggerHelperBuilder builder, Action<FileSinkOptions> configure)
        => builder.ConfigureSink("File", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

internal sealed class FileSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "File", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<FileSinkOptions>("File")
                   ?? options.BindSinkSection<FileSinkOptions>("File")
                   ?? new FileSinkOptions();

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
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new FileSinkPlugin());
}
