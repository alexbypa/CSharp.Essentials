using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Diagnostics;
using CSharpEssentials.LoggerHelper.MCP;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Overhead benchmark for LoggerHelperMcpTools (v5.0.9 killer feature).
///
/// Compares:
///   1. Direct API: read the store directly (zero-overhead baseline)
///   2. MCP tool text rendering: includes string building and formatting
///
/// These tools are called by AI assistants (not on hot paths), so the
/// goal is to confirm overhead is in the microsecond range — not milliseconds.
///
/// HOW TO RUN:
///   dotnet run -c Release --framework net9.0 -- --filter *McpTools* --job short
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class McpToolsBenchmark {
    private LoggerHelperMcpTools _tools = null!;
    private ILogErrorStore       _errorStore = null!;

    [Params(0, 10, 100)]
    public int ErrorCount;

    [GlobalSetup]
    public void Setup() {
        _errorStore = new LogErrorStore();

        // LoadedSinkStore.Add() is internal — use DI pipeline to get a pre-populated store.
        // For this benchmark we use an empty store (fastest path) to measure pure tool overhead.
        var sinkStore = new LoadedSinkStore();

        var options = new LoggerHelperOptions {
            ApplicationName = "Benchmark",
            Routes = [
                new SinkRouting { Sink = "Console", Levels = ["Information", "Warning"] },
                new SinkRouting { Sink = "File",    Levels = ["Error", "Fatal"] }
            ]
        };

        for (int i = 0; i < ErrorCount; i++)
            _errorStore.Add(new LogErrorEntry { SinkName = "Email", ErrorMessage = $"SMTP timeout #{i:D3}" });

        _tools = new LoggerHelperMcpTools(_errorStore, sinkStore, options);
    }

    [Benchmark(Baseline = true, Description = "Direct API: ILogErrorStore.GetAll()")]
    public int DirectApi_GetAll() => _errorStore.GetAll().Count;

    [Benchmark(Description = "MCP: loggerhelper_get_health")]
    public string Mcp_GetHealth() => _tools.GetHealth();

    [Benchmark(Description = "MCP: loggerhelper_get_errors")]
    public string Mcp_GetErrors() => _tools.GetErrors(count: 20);

    [Benchmark(Description = "MCP: loggerhelper_get_sinks")]
    public string Mcp_GetSinks() => _tools.GetSinks();

    [Benchmark(Description = "MCP: loggerhelper_get_config")]
    public string Mcp_GetConfig() => _tools.GetConfig();
}
