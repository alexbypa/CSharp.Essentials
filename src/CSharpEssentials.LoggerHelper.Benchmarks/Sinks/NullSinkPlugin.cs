using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Sinks;

/// <summary>
/// ISinkPlugin no-op per i benchmark — gestisce qualsiasi route il cui nome inizia con "Null".
/// Registrato via [ModuleInitializer] (entry assembly — affidabile).
/// </summary>
internal sealed class NullSinkPlugin : ISinkPlugin
{
    [ModuleInitializer]
    internal static void Register() => SinkPluginRegistry.Register(new NullSinkPlugin());

    public bool CanHandle(string sinkName) =>
        sinkName.StartsWith("Null", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options)
    {
        var levels = routing.Levels
            .Select(l => Enum.Parse<LogEventLevel>(l, ignoreCase: true))
            .ToHashSet();

        loggerConfig.WriteTo.Logger(lc =>
            lc.Filter.ByIncludingOnly(e => levels.Contains(e.Level))
              .WriteTo.Sink(new NullSink()));
    }
}
