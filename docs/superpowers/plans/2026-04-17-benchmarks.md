# BenchmarkDotNet Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Riscrivere il progetto benchmark da zero con confronto LoggerHelper v5 vs Serilog raw vs NLog, automazione GitHub Actions e pubblicazione su `docs/benchmarks.md`.

**Architecture:** Il progetto `CSharpEssentials.LoggerHelper.Benchmarks` è riscritto con sottocartelle `Sinks/`, `Competitors/`, `Benchmarks/`. Ogni competitor ha la propria classe isolata. Un `NullSinkPlugin` consente a LoggerHelper di usare un sink no-op per confronti fair (nessun I/O). GitHub Actions genera `docs/benchmarks.md` ad ogni tag `v*`.

**Tech Stack:** BenchmarkDotNet 0.14+, Serilog, NLog 5.x, Microsoft.Extensions.Logging, GitHub Actions

---

## File Map

| Azione | File |
|--------|------|
| Modify | `src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj` |
| Create | `src/CSharpEssentials.LoggerHelper.Benchmarks/Sinks/NullSink.cs` |
| Create | `src/CSharpEssentials.LoggerHelper.Benchmarks/Sinks/NullSinkPlugin.cs` |
| Delete | `src/CSharpEssentials.LoggerHelper.Benchmarks/NullSink.cs` (spostato in Sinks/) |
| Create | `src/CSharpEssentials.LoggerHelper.Benchmarks/Competitors/SerilogCompetitor.cs` |
| Create | `src/CSharpEssentials.LoggerHelper.Benchmarks/Competitors/NLogCompetitor.cs` |
| Overwrite | `src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/ThroughputBenchmark.cs` |
| Overwrite | `src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/RoutingBenchmark.cs` |
| Overwrite | `src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/StartupBenchmark.cs` |
| Delete | `src/CSharpEssentials.LoggerHelper.Benchmarks/LoggingThroughputBenchmark.cs` (rinominato) |
| Delete | `src/CSharpEssentials.LoggerHelper.Benchmarks/RoutingOverheadBenchmark.cs` (rinominato) |
| Delete | `src/CSharpEssentials.LoggerHelper.Benchmarks/StartupBenchmark.cs` (spostato) |
| Keep | `src/CSharpEssentials.LoggerHelper.Benchmarks/Program.cs` (invariato) |
| Create | `.github/workflows/benchmarks.yml` |
| Create | `.github/scripts/generate-benchmarks-md.sh` |
| Modify | `src/CSharpEssentials.LoggerHelper/README.md` |

---

## Task 1: Fix .csproj + aggiunta NLog

**Files:**
- Modify: `src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj`

- [ ] **Step 1: Riscrivere il .csproj** — rimuove il `c:` davanti a `<PackageReference>` (typo XML) e aggiunge NLog

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
    <PackageReference Include="NLog" Version="5.3.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CSharpEssentials.LoggerHelper\CSharpEssentials.LoggerHelper.csproj" />
    <ProjectReference Include="..\CSharpEssentials.LoggerHelper.Sink.Console\CSharpEssentials.LoggerHelper.Sink.Console.csproj" />
    <ProjectReference Include="..\CSharpEssentials.LoggerHelper.Sink.File\CSharpEssentials.LoggerHelper.Sink.File.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Verificare che il restore funzioni**

```bash
dotnet restore src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj
```

Expected: `Restore completed` senza errori.

- [ ] **Step 3: Commit**

```bash
git add src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj
git commit -m "fix(benchmarks): fix csproj XML typo, add NLog dependency"
```

---

## Task 2: Creare Sinks/NullSink.cs + Sinks/NullSinkPlugin.cs

**Files:**
- Create: `src/CSharpEssentials.LoggerHelper.Benchmarks/Sinks/NullSink.cs`
- Create: `src/CSharpEssentials.LoggerHelper.Benchmarks/Sinks/NullSinkPlugin.cs`
- Delete: `src/CSharpEssentials.LoggerHelper.Benchmarks/NullSink.cs`

- [ ] **Step 1: Creare `Sinks/NullSink.cs`** — sink Serilog no-op, identico all'originale ma nella nuova cartella

```csharp
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
```

- [ ] **Step 2: Creare `Sinks/NullSinkPlugin.cs`** — ISinkPlugin no-op che consente a LoggerHelper di usare il route "Null" senza I/O

```csharp
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
```

- [ ] **Step 3: Eliminare il vecchio `NullSink.cs` dalla root**

```bash
git rm src/CSharpEssentials.LoggerHelper.Benchmarks/NullSink.cs
```

- [ ] **Step 4: Build veloce per verificare che non ci siano reference rotte**

```bash
dotnet build src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj -c Release 2>&1 | tail -5
```

Expected: `Build succeeded` (alcuni warning su file non ancora creati sono accettabili in questa fase).

- [ ] **Step 5: Commit**

```bash
git add src/CSharpEssentials.LoggerHelper.Benchmarks/Sinks/
git commit -m "feat(benchmarks): add NullSink + NullSinkPlugin in Sinks/ subfolder"
```

---

## Task 3: Creare Competitors/SerilogCompetitor.cs

**Files:**
- Create: `src/CSharpEssentials.LoggerHelper.Benchmarks/Competitors/SerilogCompetitor.cs`

- [ ] **Step 1: Creare `SerilogCompetitor.cs`** — single NullSink, usato come baseline. Il multi-sink viene costruito inline nel RoutingBenchmark (Task 6) per maggiore leggibilità.

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add src/CSharpEssentials.LoggerHelper.Benchmarks/Competitors/SerilogCompetitor.cs
git commit -m "feat(benchmarks): add SerilogCompetitor"
```

---

## Task 4: Creare Competitors/NLogCompetitor.cs

**Files:**
- Create: `src/CSharpEssentials.LoggerHelper.Benchmarks/Competitors/NLogCompetitor.cs`

- [ ] **Step 1: Creare `NLogCompetitor.cs`**

```csharp
using NLog;
using NLog.Config;
using NLog.Targets;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Competitors;

/// <summary>
/// Setup isolato per NLog — usa LogFactory (non il singleton globale)
/// così più configurazioni possono coesistere nello stesso processo.
/// </summary>
internal sealed class NLogCompetitor : IDisposable
{
    private readonly LogFactory _factory;

    public NLogCompetitor(Action<LoggingConfiguration> configure)
    {
        var config = new LoggingConfiguration();
        configure(config);
        _factory = new LogFactory { Configuration = config };
    }

    public Logger Logger => _factory.GetLogger("Benchmark");

    public void Dispose() => _factory.Shutdown();

    // --- Factory methods per i casi d'uso comuni ---

    /// <summary>Single NullTarget — minimo overhead, usato come baseline NLog.</summary>
    public static NLogCompetitor SingleTarget() =>
        new(cfg =>
        {
            var t = new NullTarget("null");
            cfg.AddRule(LogLevel.Info, LogLevel.Fatal, t);
        });

    /// <summary>
    /// Due NullTarget con regole separate: primo per Info-Error, secondo per Error-Fatal.
    /// Simula routing multi-sink per confronto con LoggerHelper.
    /// </summary>
    public static NLogCompetitor MultiTarget() =>
        new(cfg =>
        {
            var t1 = new NullTarget("null-info-error");
            var t2 = new NullTarget("null-error-fatal");
            cfg.AddRule(LogLevel.Info, LogLevel.Error, t1);
            cfg.AddRule(LogLevel.Error, LogLevel.Fatal, t2);
        });
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CSharpEssentials.LoggerHelper.Benchmarks/Competitors/NLogCompetitor.cs
git commit -m "feat(benchmarks): add NLogCompetitor with LogFactory isolation"
```

---

## Task 5: Riscrivere Benchmarks/ThroughputBenchmark.cs

**Files:**
- Create: `src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/ThroughputBenchmark.cs`
- Delete: `src/CSharpEssentials.LoggerHelper.Benchmarks/LoggingThroughputBenchmark.cs`

- [ ] **Step 1: Eliminare il vecchio file**

```bash
git rm src/CSharpEssentials.LoggerHelper.Benchmarks/LoggingThroughputBenchmark.cs
```

- [ ] **Step 2: Creare `Benchmarks/ThroughputBenchmark.cs`**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Competitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Confronto throughput: LoggerHelper v5 vs Serilog raw (baseline) vs NLog.
/// Tutti usano sink no-op — misura overhead del framework, non I/O.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ThroughputBenchmark
{
    private SerilogCompetitor _serilog = null!;
    private NLogCompetitor _nlog = null!;
    private Microsoft.Extensions.Logging.ILogger _loggerHelper = null!;
    private ServiceProvider _sp = null!;

    [GlobalSetup]
    public void Setup()
    {
        _serilog = new SerilogCompetitor();
        _nlog = NLogCompetitor.SingleTarget();

        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("Benchmark")
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));
        _sp = services.BuildServiceProvider();
        _loggerHelper = _sp.GetRequiredService<ILoggerProvider>().CreateLogger("Benchmark");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serilog.Dispose();
        _nlog.Dispose();
        _sp.Dispose();
    }

    // --- Single message ---

    [Benchmark(Baseline = true)]
    public void Serilog_SingleMessage()
        => _serilog.Logger.Information("Benchmark message {Counter}", 42);

    [Benchmark]
    public void NLog_SingleMessage()
        => _nlog.Logger.Info("Benchmark message {Counter}", 42);

    [Benchmark]
    public void LoggerHelper_SingleMessage()
        => _loggerHelper.LogInformation("Benchmark message {Counter}", 42);

    // --- Structured payload (3 properties) ---

    [Benchmark]
    public void Serilog_StructuredPayload()
        => _serilog.Logger.Information(
            "Order {OrderId} for {Customer} total {Amount}",
            12345, "Acme Corp", 99.99m);

    [Benchmark]
    public void NLog_StructuredPayload()
        => _nlog.Logger.Info(
            "Order {OrderId} for {Customer} total {Amount}",
            12345, "Acme Corp", 99.99m);

    [Benchmark]
    public void LoggerHelper_StructuredPayload()
        => _loggerHelper.LogInformation(
            "Order {OrderId} for {Customer} total {Amount}",
            12345, "Acme Corp", 99.99m);

    // --- Below MinLevel (Debug filtered out in prod) ---

    [Benchmark]
    public void Serilog_BelowMinLevel()
        => _serilog.Logger.Debug("This should be filtered {Value}", 1);

    [Benchmark]
    public void NLog_BelowMinLevel()
        => _nlog.Logger.Debug("This should be filtered {Value}", 1);

    [Benchmark]
    public void LoggerHelper_BelowMinLevel()
        => _loggerHelper.LogDebug("This should be filtered {Value}", 1);
}
```

- [ ] **Step 3: Build di verifica**

```bash
dotnet build src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj -c Release 2>&1 | tail -5
```

Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/ThroughputBenchmark.cs
git commit -m "feat(benchmarks): add ThroughputBenchmark (Serilog/NLog/LoggerHelper)"
```

---

## Task 6: Riscrivere Benchmarks/RoutingBenchmark.cs

**Files:**
- Create: `src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/RoutingBenchmark.cs`
- Delete: `src/CSharpEssentials.LoggerHelper.Benchmarks/RoutingOverheadBenchmark.cs`

- [ ] **Step 1: Eliminare il vecchio file**

```bash
git rm src/CSharpEssentials.LoggerHelper.Benchmarks/RoutingOverheadBenchmark.cs
```

- [ ] **Step 2: Creare `Benchmarks/RoutingBenchmark.cs`**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Competitors;
using CSharpEssentials.LoggerHelper.Benchmarks.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Routing overhead: single-sink vs multi-sink per LoggerHelper, Serilog, NLog.
/// Differenziatore chiave di LoggerHelper: routing dichiarativo per livello.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RoutingBenchmark
{
    // LoggerHelper
    private Microsoft.Extensions.Logging.ILogger _lhSingle = null!;
    private Microsoft.Extensions.Logging.ILogger _lhMulti = null!;
    private ServiceProvider _sp1 = null!;
    private ServiceProvider _sp2 = null!;

    // Serilog
    private Serilog.ILogger _serilogSingle = null!;
    private Serilog.ILogger _serilogMulti = null!;
    private Logger _serilogSingleRoot = null!;
    private Logger _serilogMultiRoot = null!;

    // NLog
    private NLogCompetitor _nlogSingle = null!;
    private NLogCompetitor _nlogMulti = null!;

    [GlobalSetup]
    public void Setup()
    {
        // LoggerHelper — single route
        var s1 = new ServiceCollection();
        s1.AddLoggerHelper(b => b
            .WithApplicationName("SingleRoute")
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));
        _sp1 = s1.BuildServiceProvider();
        _lhSingle = _sp1.GetRequiredService<ILoggerProvider>().CreateLogger("Single");

        // LoggerHelper — multi route (NullA: Info/Warning/Error, NullB: Error/Fatal)
        var s2 = new ServiceCollection();
        s2.AddLoggerHelper(b => b
            .WithApplicationName("MultiRoute")
            .AddRoute("NullA", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error)
            .AddRoute("NullB", LogEventLevel.Error, LogEventLevel.Fatal));
        _sp2 = s2.BuildServiceProvider();
        _lhMulti = _sp2.GetRequiredService<ILoggerProvider>().CreateLogger("Multi");

        // Serilog — single sink
        _serilogSingleRoot = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(new NullSink())
            .CreateLogger();
        _serilogSingle = _serilogSingleRoot;

        // Serilog — multi sub-logger (Console: Info-Error, File: Error-Fatal)
        _serilogMultiRoot = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e =>
                    e.Level >= LogEventLevel.Information &&
                    e.Level <= LogEventLevel.Error)
                .WriteTo.Sink(new NullSink()))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                .WriteTo.Sink(new NullSink()))
            .CreateLogger();
        _serilogMulti = _serilogMultiRoot;

        // NLog
        _nlogSingle = NLogCompetitor.SingleTarget();
        _nlogMulti = NLogCompetitor.MultiTarget();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _sp1.Dispose();
        _sp2.Dispose();
        _serilogSingleRoot.Dispose();
        _serilogMultiRoot.Dispose();
        _nlogSingle.Dispose();
        _nlogMulti.Dispose();
    }

    // --- Single sink/target/route baseline ---

    [Benchmark(Baseline = true)]
    public void Serilog_Single_Info()
        => _serilogSingle.Information("Routing test {Value}", 1);

    [Benchmark]
    public void NLog_Single_Info()
        => _nlogSingle.Logger.Info("Routing test {Value}", 1);

    [Benchmark]
    public void LoggerHelper_Single_Info()
        => _lhSingle.LogInformation("Routing test {Value}", 1);

    // --- Multi sink: messaggio Info → 1 destinazione ---

    [Benchmark]
    public void Serilog_Multi_Info()
        => _serilogMulti.Information("Routing test {Value}", 1);

    [Benchmark]
    public void NLog_Multi_Info()
        => _nlogMulti.Logger.Info("Routing test {Value}", 1);

    [Benchmark]
    public void LoggerHelper_Multi_Info()
        => _lhMulti.LogInformation("Routing test {Value}", 1);

    // --- Multi sink: messaggio Error → 2 destinazioni ---

    [Benchmark]
    public void Serilog_Multi_Error()
        => _serilogMulti.Error("Routing test {Value}", 1);

    [Benchmark]
    public void NLog_Multi_Error()
        => _nlogMulti.Logger.Error("Routing test {Value}", 1);

    [Benchmark]
    public void LoggerHelper_Multi_Error()
        => _lhMulti.LogError("Routing test {Value}", 1);
}
```

- [ ] **Step 3: Build di verifica**

```bash
dotnet build src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj -c Release 2>&1 | tail -5
```

Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/RoutingBenchmark.cs
git commit -m "feat(benchmarks): add RoutingBenchmark (single vs multi-sink)"
```

---

## Task 7: Riscrivere Benchmarks/StartupBenchmark.cs

**Files:**
- Create: `src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/StartupBenchmark.cs`
- Delete: `src/CSharpEssentials.LoggerHelper.Benchmarks/StartupBenchmark.cs` (root)

- [ ] **Step 1: Eliminare il vecchio file dalla root**

```bash
git rm src/CSharpEssentials.LoggerHelper.Benchmarks/StartupBenchmark.cs
```

- [ ] **Step 2: Creare `Benchmarks/StartupBenchmark.cs`**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CSharpEssentials.LoggerHelper.Benchmarks.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Targets;
using Serilog;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Benchmarks;

/// <summary>
/// Costo di inizializzazione — rilevante per Azure Functions e cold start lambda.
/// Ogni invocazione crea e distrugge un'istanza del logger.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StartupBenchmark
{
    [Benchmark(Baseline = true)]
    public void Serilog_Startup()
    {
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(new NullSink())
            .CreateLogger();
    }

    [Benchmark]
    public void NLog_Startup()
    {
        var config = new LoggingConfiguration();
        var nullTarget = new NullTarget("null");
        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, nullTarget);
        var factory = new NLog.LogFactory { Configuration = config };
        _ = factory.GetLogger("StartupBench");
        factory.Shutdown();
    }

    [Benchmark]
    public void LoggerHelper_Startup()
    {
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("StartupBench")
            .AddRoute("Null",
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal));
        using var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<ILoggerProvider>().CreateLogger("StartupBench");
    }
}
```

- [ ] **Step 3: Build finale dell'intero progetto**

```bash
dotnet build src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj -c Release 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add src/CSharpEssentials.LoggerHelper.Benchmarks/Benchmarks/StartupBenchmark.cs
git commit -m "feat(benchmarks): add StartupBenchmark (Serilog/NLog/LoggerHelper)"
```

---

## Task 8: Smoke test — dry run

- [ ] **Step 1: Eseguire un dry-run per verificare che tutti i benchmark si avviino senza errori**

```bash
dotnet run -c Release \
  --project src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj \
  -- --filter * --job Dry
```

Expected: ogni benchmark classe stampa una tabella con una sola riga (1 iterazione), nessuna eccezione, terminazione con exit code 0.

- [ ] **Step 2: Se ci sono errori nel dry-run** — leggere lo stack trace, identificare quale benchmark/setup fallisce, correggere e ripetere Step 1.

- [ ] **Step 3: Commit del fix (solo se Step 2 richiesto)**

```bash
git add -p
git commit -m "fix(benchmarks): fix dry-run errors"
```

---

## Task 9: GitHub Actions workflow

**Files:**
- Create: `.github/workflows/benchmarks.yml`

- [ ] **Step 1: Verificare che `.github/workflows/` esista**

```bash
ls .github/workflows/
```

Expected: directory esistente con almeno `publish.yml`.

- [ ] **Step 2: Creare `.github/workflows/benchmarks.yml`**

```yaml
name: Benchmarks

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:

permissions:
  contents: write

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore
        run: dotnet restore src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj

      - name: Build Release
        run: dotnet build src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj -c Release --no-restore

      - name: Run benchmarks
        run: |
          dotnet run -c Release \
            --project src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj \
            --no-build \
            -- --filter * --exporters github --artifacts ./benchmark-results

      - name: Generate docs/benchmarks.md
        run: bash .github/scripts/generate-benchmarks-md.sh

      - name: Commit benchmark results
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add docs/benchmarks.md
          git diff --staged --quiet || git commit -m "chore: update benchmark results [skip ci]"
          git push
```

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/benchmarks.yml
git commit -m "ci: add benchmarks workflow (triggers on tag v*)"
```

---

## Task 10: Script di generazione docs/benchmarks.md

**Files:**
- Create: `.github/scripts/generate-benchmarks-md.sh`

- [ ] **Step 1: Creare la cartella `.github/scripts/` se non esiste**

```bash
mkdir -p .github/scripts
```

- [ ] **Step 2: Creare `.github/scripts/generate-benchmarks-md.sh`**

```bash
#!/usr/bin/env bash
set -euo pipefail

ARTIFACTS_DIR="benchmark-results/results"
OUTPUT="docs/benchmarks.md"

mkdir -p docs

{
  echo "# LoggerHelper v5 — Benchmark Results"
  echo ""
  echo "> Generated: $(date -u '+%Y-%m-%d') | Runtime: .NET 9 | OS: ubuntu-latest"
  echo ""
  echo "Comparison: **LoggerHelper v5** vs **Serilog** (baseline) vs **NLog**."
  echo "All frameworks use a no-op sink/target — measures framework overhead, not I/O."
  echo ""
  echo "---"
  echo ""
  echo "## Throughput"
  echo ""
  if ls "$ARTIFACTS_DIR"/*ThroughputBenchmark-report-github.md 1>/dev/null 2>&1; then
    cat "$ARTIFACTS_DIR"/*ThroughputBenchmark-report-github.md
  else
    echo "_Results not available._"
  fi
  echo ""
  echo "---"
  echo ""
  echo "## Routing Overhead"
  echo ""
  if ls "$ARTIFACTS_DIR"/*RoutingBenchmark-report-github.md 1>/dev/null 2>&1; then
    cat "$ARTIFACTS_DIR"/*RoutingBenchmark-report-github.md
  else
    echo "_Results not available._"
  fi
  echo ""
  echo "---"
  echo ""
  echo "## Startup Time"
  echo ""
  if ls "$ARTIFACTS_DIR"/*StartupBenchmark-report-github.md 1>/dev/null 2>&1; then
    cat "$ARTIFACTS_DIR"/*StartupBenchmark-report-github.md
  else
    echo "_Results not available._"
  fi
  echo ""
  echo "---"
  echo ""
  echo "_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._"
} > "$OUTPUT"

echo "Generated $OUTPUT"
```

- [ ] **Step 3: Rendere lo script eseguibile**

```bash
chmod +x .github/scripts/generate-benchmarks-md.sh
```

- [ ] **Step 4: Creare un `docs/benchmarks.md` placeholder** — così il file esiste già nel repo prima del primo CI run

```markdown
# LoggerHelper v5 — Benchmark Results

> Benchmarks run automatically on each release. Results will appear here after the first `v*` tag is pushed.

Comparison: **LoggerHelper v5** vs **Serilog** vs **NLog**.

---

_Run locally:_

```bash
dotnet run -c Release \
  --project src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj \
  -- --filter * --exporters github
```
```

- [ ] **Step 5: Commit**

```bash
git add .github/scripts/generate-benchmarks-md.sh docs/benchmarks.md
git commit -m "ci: add benchmark result generation script + placeholder docs/benchmarks.md"
```

---

## Task 11: Aggiornare README con sezione Performance

**Files:**
- Modify: `src/CSharpEssentials.LoggerHelper/README.md`

- [ ] **Step 1: Leggere il README corrente** per trovare il punto di inserimento corretto (dopo la sezione Quick Start, prima di Contributing o License).

- [ ] **Step 2: Aggiungere la sezione Performance**

Trovare la riga dove inserire (tipicamente dopo la sezione di configurazione) e aggiungere:

```markdown
## Performance

LoggerHelper v5 is built on Serilog, adding a thin MEL-compatible routing layer.
Overhead vs raw Serilog is kept minimal — no reflection at log time, level checks
short-circuit before routing evaluation.

> Full benchmark results (auto-updated on each release) → **[docs/benchmarks.md](../../docs/benchmarks.md)**

Benchmarks run with [BenchmarkDotNet](https://benchmarkdotnet.org/) on .NET 9,
comparing LoggerHelper v5 against Serilog (baseline) and NLog using no-op sinks
to measure framework overhead independently of I/O.
```

- [ ] **Step 3: Commit**

```bash
git add src/CSharpEssentials.LoggerHelper/README.md
git commit -m "docs: add Performance section to README linking benchmarks page"
```

---

## Task 12: Verifica finale + tag di release per triggherare CI

- [ ] **Step 1: Build completo della soluzione**

```bash
dotnet build src/CSharpEssentials.LoggerHelper.slnx -c Release 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2: Dry-run finale**

```bash
dotnet run -c Release \
  --project src/CSharpEssentials.LoggerHelper.Benchmarks/CSharpEssentials.LoggerHelper.Benchmarks.csproj \
  -- --filter * --job Dry 2>&1 | grep -E "(// \*|Error|error|succeeded)"
```

Expected: righe con `// *` per ogni benchmark class, nessun `Error`.

- [ ] **Step 3: Verificare git log**

```bash
git log --oneline -10
```

Expected: tutti i commit dei task precedenti in ordine.

- [ ] **Step 4 (opzionale — richede conferma utente): Creare tag per triggherare CI**

```bash
git tag v5.0.0-benchmarks
git push origin v5.0.0-benchmarks
```

> **Attenzione:** questo triggera il workflow CI che fa girare i benchmark completi (10-20 minuti su ubuntu-latest) e committa i risultati su main. Eseguire solo se i benchmark sono stati verificati con dry-run.

---

## Note di esecuzione

- **Ordine obbligatorio:** Task 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12
- **Task 8 (smoke test)** è il gate — se fallisce, non procedere con CI/README
- **Build deve essere `-c Release`** — BenchmarkDotNet rifiuta Debug con warning
- **`SinkPluginRegistry` è un singleton statico** — se più test/benchmark condividono il processo, i plugin accumulano. Il NullSinkPlugin gestisce questo via `CanHandle` prefix-match
- **NLog LogFactory** è isolata per istanza — nessun conflitto tra single e multi competitor nello stesso processo
