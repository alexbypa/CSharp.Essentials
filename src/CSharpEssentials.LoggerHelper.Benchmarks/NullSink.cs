using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks;

/// <summary>
/// A no-op Serilog sink used in benchmarks to isolate
/// logging overhead from actual I/O cost.
/// </summary>
internal sealed class NullSink : ILogEventSink
{
    public void Emit(LogEvent logEvent) { }
}
