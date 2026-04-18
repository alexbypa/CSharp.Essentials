using System.Diagnostics;
using CSharpEssentials.LoggerHelper.Benchmarks.Competitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks;

/// <summary>
/// Long-running soak test that detects memory leaks by monitoring GC heap size over time.
///
/// NOT a BenchmarkDotNet benchmark — this is a standalone stress test that:
///   1. Exercises all logging APIs (ILogger, BeginTrace, Trace) in a tight loop
///   2. Takes heap snapshots every 10 seconds after forcing GC
///   3. Skips the first 2 minutes (JIT warmup)
///   4. Runs linear regression on post-warmup snapshots
///   5. Reports PASS/FAIL based on memory growth slope
///   6. Saves CSV + summary report to BenchmarkDotNet.Artifacts/memory-leak-test/
///
/// Usage:
///   dotnet run -c Release --framework net9.0 -- --leak-test              (default 30 min)
///   dotnet run -c Release --framework net9.0 -- --leak-test --duration 5 (5 min quick check)
/// </summary>
public static class MemoryLeakTest {
    private const int SnapshotIntervalSeconds = 10;
    private const int WarmupMinutes = 2;

    /// <summary>Memory growth threshold: above this = leak detected.</summary>
    private const double LeakThresholdBytesPerMinute = 10_240; // 10 KB/min

    /// <summary>Warning threshold: might be a slow leak.</summary>
    private const double WarnThresholdBytesPerMinute = 1_024; // 1 KB/min

    public static async Task<int> RunAsync(int durationMinutes) {
        Console.WriteLine($"=== Memory Leak Soak Test ===");
        Console.WriteLine($"Duration: {durationMinutes} min | Snapshots every {SnapshotIntervalSeconds}s | Warmup: {WarmupMinutes} min");
        Console.WriteLine();

        // ── Setup ──────────────────────────────────────────────────
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("LeakTest")
            .DisableOpenTelemetry()
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerProvider>().CreateLogger("LeakTest");

        var serilog = new SerilogCompetitor();
        var nlog = NLogCompetitor.SingleTarget();

        var testEx = new InvalidOperationException("soak-test");

        // ── Run ────────────────────────────────────────────────────
        var snapshots = new List<MemorySnapshot>();
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(durationMinutes));
        var sw = Stopwatch.StartNew();
        long iteration = 0;
        int nextSnapshotSec = 0;

        TakeSnapshot(snapshots, sw);
        PrintHeader();

        try {
            while (!cts.IsCancellationRequested) {
                // ── Exercise ALL logging APIs ──────────────────────

                // 1. ILogger direct (most common path)
                logger.LogInformation("Order {OrderId} total {Amount}", iteration, 49.99m);

                // 2. BeginTrace scope (scope lifecycle + dispose)
                using (logger.BeginTrace("SoakProcess", $"TXN-{iteration % 100_000}")) {
                    logger.LogInformation("Step 1: validate {OrderId}", iteration);
                    logger.LogWarning("Step 2: low stock {Qty}", 2);
                    logger.LogError(testEx, "Step 3: payment failed {OrderId}", iteration);
                }

                // 3. Trace single-shot (template appending)
                logger.Trace("SoakAction", $"TXN-{iteration % 100_000}",
                    LogLevel.Information, null,
                    "Soak {Iter} for {Customer}", iteration, "Acme");

                // 4. Trace shorthand
                logger.Trace("SoakShort", $"TXN-{iteration % 100_000}",
                    "Quick {Iter}", iteration);

                // 5. Serilog raw (reference — should be flat)
                serilog.Logger.Information("Serilog soak {Iter}", iteration);

                // 6. NLog raw (reference — should be flat)
                nlog.Logger.Info("NLog soak {Iter}", iteration);

                iteration++;

                // ── Periodic snapshot ──────────────────────────────
                if (sw.Elapsed.TotalSeconds >= nextSnapshotSec) {
                    TakeSnapshot(snapshots, sw);
                    PrintRow(snapshots[^1], iteration, durationMinutes);
                    nextSnapshotSec += SnapshotIntervalSeconds;
                }
            }
        } catch (OperationCanceledException) { }

        sw.Stop();

        // Final snapshot
        TakeSnapshot(snapshots, sw);
        PrintRow(snapshots[^1], iteration, durationMinutes);

        serilog.Dispose();
        nlog.Dispose();

        // ── Analyze ────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine($"Completed: {iteration:N0} iterations in {sw.Elapsed.TotalMinutes:F1} min");
        Console.WriteLine($"Rate: {iteration / sw.Elapsed.TotalSeconds:N0} logs/sec (6 log calls per iteration)");
        Console.WriteLine();

        var result = Analyze(snapshots);

        // ── Save results ───────────────────────────────────────────
        var outputDir = Path.Combine("BenchmarkDotNet.Artifacts", "memory-leak-test");
        Directory.CreateDirectory(outputDir);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var csvPath = Path.Combine(outputDir, $"snapshots_{timestamp}.csv");
        var reportPath = Path.Combine(outputDir, $"report_{timestamp}.md");

        SaveCsv(snapshots, csvPath);
        SaveReport(snapshots, result, iteration, sw.Elapsed, reportPath);

        Console.WriteLine($"CSV:    {Path.GetFullPath(csvPath)}");
        Console.WriteLine($"Report: {Path.GetFullPath(reportPath)}");

        return result.Verdict == LeakVerdict.Leak ? 1 : 0;
    }

    // ── Snapshot ───────────────────────────────────────────────────

    private static void TakeSnapshot(List<MemorySnapshot> snapshots, Stopwatch sw) {
        // Force full GC to get accurate managed heap size
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        snapshots.Add(new MemorySnapshot {
            Elapsed = sw.Elapsed,
            ManagedHeap = GC.GetTotalMemory(forceFullCollection: false),
            WorkingSet = Process.GetCurrentProcess().WorkingSet64,
            Gen0 = GC.CollectionCount(0),
            Gen1 = GC.CollectionCount(1),
            Gen2 = GC.CollectionCount(2)
        });
    }

    // ── Console output ─────────────────────────────────────────────

    private static void PrintHeader() {
        Console.WriteLine("  Elapsed |   Managed Heap |   Working Set |  Gen0 |  Gen1 |  Gen2 | Iterations");
        Console.WriteLine(" ---------|----------------|---------------|-------|-------|-------|------------");
    }

    private static void PrintRow(MemorySnapshot s, long iterations, int totalMin) {
        var pct = s.Elapsed.TotalMinutes / totalMin * 100;
        Console.Write($"  {s.Elapsed:mm\\:ss}  |");
        Console.Write($"  {s.ManagedHeap / 1024.0 / 1024.0,10:F2} MB |");
        Console.Write($"  {s.WorkingSet / 1024.0 / 1024.0,9:F2} MB |");
        Console.Write($"  {s.Gen0,4} |  {s.Gen1,4} |  {s.Gen2,4} |");
        Console.Write($" {iterations,10:N0}");
        Console.WriteLine($"  ({pct:F0}%)");
    }

    // ── Analysis ───────────────────────────────────────────────────

    private static AnalysisResult Analyze(List<MemorySnapshot> snapshots) {
        // Skip warmup period
        var warmupThreshold = TimeSpan.FromMinutes(WarmupMinutes);
        var postWarmup = snapshots.Where(s => s.Elapsed >= warmupThreshold).ToList();

        if (postWarmup.Count < 3) {
            Console.WriteLine("WARNING: Not enough post-warmup snapshots for analysis (need at least 3).");
            Console.WriteLine($"  Total snapshots: {snapshots.Count}, post-warmup: {postWarmup.Count}");
            Console.WriteLine("  Increase duration or decrease snapshot interval.");
            return new AnalysisResult {
                Verdict = LeakVerdict.Inconclusive,
                SlopeBytesPerMinute = 0,
                R2 = 0,
                MinHeap = snapshots.Min(s => s.ManagedHeap),
                MaxHeap = snapshots.Max(s => s.ManagedHeap),
                Message = "Not enough data points for analysis"
            };
        }

        // Linear regression: y = managed heap bytes, x = elapsed minutes
        double[] x = postWarmup.Select(s => s.Elapsed.TotalMinutes).ToArray();
        double[] y = postWarmup.Select(s => (double)s.ManagedHeap).ToArray();

        var (slope, intercept, r2) = LinearRegression(x, y);

        var minHeap = postWarmup.Min(s => s.ManagedHeap);
        var maxHeap = postWarmup.Max(s => s.ManagedHeap);
        var heapRange = maxHeap - minHeap;

        LeakVerdict verdict;
        string message;

        if (slope > LeakThresholdBytesPerMinute && r2 > 0.7) {
            verdict = LeakVerdict.Leak;
            message = $"MEMORY LEAK DETECTED: heap growing {slope / 1024:F1} KB/min (R²={r2:F3})";
            Console.ForegroundColor = ConsoleColor.Red;
        } else if (slope > WarnThresholdBytesPerMinute && r2 > 0.5) {
            verdict = LeakVerdict.Warning;
            message = $"WARNING: possible slow leak, heap growing {slope / 1024:F1} KB/min (R²={r2:F3})";
            Console.ForegroundColor = ConsoleColor.Yellow;
        } else {
            verdict = LeakVerdict.Pass;
            message = $"PASS: heap stable, slope {slope / 1024:F1} KB/min (R²={r2:F3})";
            Console.ForegroundColor = ConsoleColor.Green;
        }

        Console.WriteLine($"  {message}");
        Console.ResetColor();
        Console.WriteLine($"  Heap range: {minHeap / 1024.0 / 1024.0:F2} MB — {maxHeap / 1024.0 / 1024.0:F2} MB (delta {heapRange / 1024.0:F1} KB)");
        Console.WriteLine($"  Post-warmup snapshots: {postWarmup.Count}");

        return new AnalysisResult {
            Verdict = verdict,
            SlopeBytesPerMinute = slope,
            R2 = r2,
            MinHeap = minHeap,
            MaxHeap = maxHeap,
            Message = message
        };
    }

    /// <summary>
    /// Least-squares linear regression. Returns (slope, intercept, R²).
    /// </summary>
    private static (double slope, double intercept, double r2) LinearRegression(double[] x, double[] y) {
        int n = x.Length;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

        for (int i = 0; i < n; i++) {
            sumX += x[i];
            sumY += y[i];
            sumXY += x[i] * y[i];
            sumX2 += x[i] * x[i];
            sumY2 += y[i] * y[i];
        }

        double denom = n * sumX2 - sumX * sumX;
        if (Math.Abs(denom) < 1e-10)
            return (0, sumY / n, 0);

        double slope = (n * sumXY - sumX * sumY) / denom;
        double intercept = (sumY - slope * sumX) / n;

        // R² (coefficient of determination)
        double meanY = sumY / n;
        double ssTotal = 0, ssResidual = 0;
        for (int i = 0; i < n; i++) {
            double predicted = slope * x[i] + intercept;
            ssResidual += (y[i] - predicted) * (y[i] - predicted);
            ssTotal += (y[i] - meanY) * (y[i] - meanY);
        }

        double r2 = ssTotal > 0 ? 1 - ssResidual / ssTotal : 0;
        return (slope, intercept, Math.Max(0, r2));
    }

    // ── File output ────────────────────────────────────────────────

    private static void SaveCsv(List<MemorySnapshot> snapshots, string path) {
        using var writer = new StreamWriter(path);
        writer.WriteLine("ElapsedSeconds,ManagedHeapBytes,WorkingSetBytes,Gen0,Gen1,Gen2");
        foreach (var s in snapshots) {
            writer.WriteLine($"{s.Elapsed.TotalSeconds:F1},{s.ManagedHeap},{s.WorkingSet},{s.Gen0},{s.Gen1},{s.Gen2}");
        }
    }

    private static void SaveReport(List<MemorySnapshot> snapshots, AnalysisResult result,
        long iterations, TimeSpan elapsed, string path) {
        using var writer = new StreamWriter(path);
        writer.WriteLine("# Memory Leak Soak Test Report");
        writer.WriteLine();
        writer.WriteLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"**Duration:** {elapsed.TotalMinutes:F1} minutes");
        writer.WriteLine($"**Iterations:** {iterations:N0}");
        writer.WriteLine($"**Rate:** {iterations / elapsed.TotalSeconds:N0} iterations/sec (6 log calls each)");
        writer.WriteLine($"**Runtime:** .NET {Environment.Version}");
        writer.WriteLine();

        writer.WriteLine("## Verdict");
        writer.WriteLine();
        writer.WriteLine($"**{result.Verdict}** — {result.Message}");
        writer.WriteLine();

        writer.WriteLine("## Memory Statistics");
        writer.WriteLine();
        writer.WriteLine($"| Metric | Value |");
        writer.WriteLine($"|---|---|");
        writer.WriteLine($"| Min managed heap | {result.MinHeap / 1024.0 / 1024.0:F2} MB |");
        writer.WriteLine($"| Max managed heap | {result.MaxHeap / 1024.0 / 1024.0:F2} MB |");
        writer.WriteLine($"| Heap range | {(result.MaxHeap - result.MinHeap) / 1024.0:F1} KB |");
        writer.WriteLine($"| Growth slope | {result.SlopeBytesPerMinute / 1024:F1} KB/min |");
        writer.WriteLine($"| R² (fit quality) | {result.R2:F4} |");
        writer.WriteLine($"| Thresholds | Warn > {WarnThresholdBytesPerMinute / 1024.0:F0} KB/min, Leak > {LeakThresholdBytesPerMinute / 1024.0:F0} KB/min |");
        writer.WriteLine();

        writer.WriteLine("## GC Activity");
        writer.WriteLine();
        var first = snapshots[0];
        var last = snapshots[^1];
        writer.WriteLine($"| Generation | Collections |");
        writer.WriteLine($"|---|---|");
        writer.WriteLine($"| Gen 0 | {last.Gen0 - first.Gen0} |");
        writer.WriteLine($"| Gen 1 | {last.Gen1 - first.Gen1} |");
        writer.WriteLine($"| Gen 2 | {last.Gen2 - first.Gen2} |");
        writer.WriteLine();

        writer.WriteLine("## Snapshots (first 10 + last 10)");
        writer.WriteLine();
        writer.WriteLine("| Elapsed | Managed Heap | Working Set | Gen0 | Gen1 | Gen2 |");
        writer.WriteLine("|---|---|---|---|---|---|");

        var display = snapshots.Count <= 20
            ? snapshots
            : snapshots.Take(10).Concat(snapshots.TakeLast(10)).ToList();

        bool gapPrinted = false;
        foreach (var s in display) {
            if (!gapPrinted && snapshots.Count > 20 && s == snapshots[^10]) {
                writer.WriteLine("| ... | ... | ... | ... | ... | ... |");
                gapPrinted = true;
            }
            writer.WriteLine(
                $"| {s.Elapsed:mm\\:ss} | {s.ManagedHeap / 1024.0 / 1024.0:F2} MB | {s.WorkingSet / 1024.0 / 1024.0:F2} MB | {s.Gen0} | {s.Gen1} | {s.Gen2} |");
        }
    }

    // ── Types ──────────────────────────────────────────────────────

    private record struct MemorySnapshot {
        public TimeSpan Elapsed;
        public long ManagedHeap;
        public long WorkingSet;
        public int Gen0;
        public int Gen1;
        public int Gen2;
    }

    private enum LeakVerdict { Pass, Warning, Leak, Inconclusive }

    private record AnalysisResult {
        public LeakVerdict Verdict { get; init; }
        public double SlopeBytesPerMinute { get; init; }
        public double R2 { get; init; }
        public long MinHeap { get; init; }
        public long MaxHeap { get; init; }
        public string Message { get; init; } = "";
    }
}
