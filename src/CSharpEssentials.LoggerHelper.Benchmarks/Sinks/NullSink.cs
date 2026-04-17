using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Sinks;

/// <summary>
/// Serilog no-op sink — isola l'overhead di logging dall'I/O reale.
/// </summary>
internal sealed class NullSink : ILogEventSink
{
    public void Emit(LogEvent logEvent) { }
}
