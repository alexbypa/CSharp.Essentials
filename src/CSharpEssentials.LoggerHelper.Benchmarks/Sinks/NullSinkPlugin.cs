using System.Runtime.CompilerServices;
using Serilog;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Sinks;

/// <summary>
/// ISinkPlugin no-op per i benchmark — gestisce qualsiasi route il cui nome inizia con "Null".
/// Registrato via [ModuleInitializer] (entry assembly — affidabile).
/// Usa WriteTo.Conditional + routing.Matches come i sink pubblicati (Console, File),
/// così il benchmark misura lo stesso percorso dei sink reali (nessun sotto-logger).
/// </summary>
internal sealed class NullSinkPlugin : ISinkPlugin
{
    [ModuleInitializer]
    internal static void Register() => SinkPluginRegistry.Register(new NullSinkPlugin());

    public bool CanHandle(string sinkName) =>
        sinkName.StartsWith("Null", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) =>
        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.Sink(new NullSink()));
}
