using Serilog;
using Serilog.Core;
using CSharpEssentials.LoggerHelper.Benchmarks.Sinks;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Competitors;

/// <summary>
/// Setup isolato per Serilog raw con singolo NullSink — usato come baseline.
/// </summary>
internal sealed class SerilogCompetitor : IDisposable
{
    private readonly Logger _root;

    public SerilogCompetitor()
    {
        _root = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(new NullSink())
            .CreateLogger();
    }

    public Serilog.ILogger Logger => _root;

    public void Dispose() => _root.Dispose();
}
