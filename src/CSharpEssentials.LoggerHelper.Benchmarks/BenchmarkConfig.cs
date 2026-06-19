using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace CSharpEssentials.LoggerHelper.Benchmarks;

/// <summary>
/// CI-friendly benchmark configuration: short runs, GitHub Markdown export, memory diagnostics.
/// Applied as the default config in Program.cs so all benchmarks inherit it automatically.
/// </summary>
public class CIConfig : ManualConfig {
    public CIConfig() {
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddJob(Job.ShortRun
            .WithLaunchCount(1)
            .WithWarmupCount(3)
            .WithIterationCount(5));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(RankColumn.Arabic);
    }
}
