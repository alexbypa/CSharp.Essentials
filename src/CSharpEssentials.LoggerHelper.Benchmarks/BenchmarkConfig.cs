using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace CSharpEssentials.LoggerHelper.Benchmarks;

/// <summary>
/// Benchmark configuration that limits CPU and memory usage.
///
/// Usage:
///   Quick run (low resource, ~2 min per benchmark class):
///     dotnet run -c Release --framework net9.0 -- --filter * --job short
///
///   Full run (accurate results, takes longer):
///     dotnet run -c Release --framework net9.0 -- --filter *
///
///   Single benchmark only:
///     dotnet run -c Release --framework net9.0 -- --filter *Throughput*
/// </summary>
public class LightConfig : ManualConfig {
    public LightConfig() {
        AddJob(Job.ShortRun
            .WithLaunchCount(1)
            .WithWarmupCount(3)
            .WithIterationCount(3));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(RankColumn.Arabic);
    }
}
