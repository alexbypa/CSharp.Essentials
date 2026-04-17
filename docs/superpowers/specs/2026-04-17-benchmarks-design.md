# BenchmarkDotNet — Design Spec

> Created: 2026-04-17 | Author: Alessandro Chiodo

## Obiettivo

Produrre benchmark affidabili e pubblicabili che posizionino **CSharpEssentials.LoggerHelper v5**
come soluzione competitiva (o superiore) rispetto a Serilog raw e NLog nel mercato .NET.
I risultati alimentano il README NuGet, una pagina dedicata `docs/benchmarks.md`, e vengono
aggiornati automaticamente ad ogni release tramite GitHub Actions.

**Obiettivo primario:** marketing/comunicazione — numeri convincenti e onesti.

---

## Competitor inclusi

| Competitor | Versione target | Note |
|---|---|---|
| **Serilog raw** | latest stable | Baseline — LoggerHelper si basa su Serilog, misura il costo dello strato di astrazione |
| **NLog** | latest stable | Secondo big del logging .NET, massima credibilità comparativa |

MEL puro e altri framework sono fuori scope per ora.

---

## Struttura del progetto

```
src/CSharpEssentials.LoggerHelper.Benchmarks/
├── CSharpEssentials.LoggerHelper.Benchmarks.csproj
├── Program.cs
├── Sinks/
│   └── NullSink.cs                    (Serilog ILogEventSink no-op)
├── Competitors/
│   ├── SerilogCompetitor.cs           (setup + teardown Serilog isolato)
│   └── NLogCompetitor.cs              (setup + teardown NLog isolato)
└── Benchmarks/
    ├── ThroughputBenchmark.cs
    ├── RoutingBenchmark.cs
    └── StartupBenchmark.cs
```

**Principio di separazione:** ogni competitor è una classe autonoma con metodi `Build()` e
`Dispose()`. I benchmark importano dal competitor — nessun setup inline nelle classi benchmark.
Aggiungere un quarto competitor richiede 1 file, non modificare 3 benchmark esistenti.

---

## Scenari misurati

### ThroughputBenchmark
Attributi: `[MemoryDiagnoser]`, `[Orderer(FastestToSlowest)]`, `[RankColumn]`

| Benchmark | Cosa misura |
|---|---|
| `RawSerilog_SingleMessage` | Serilog puro, NullSink — **baseline** |
| `NLog_SingleMessage` | NLog con NullTarget |
| `LoggerHelper_SingleMessage` | LoggerHelper v5, NullSink |
| `RawSerilog_StructuredPayload` | 3 properties tipizzate (`{OrderId}`, `{Customer}`, `{Amount}`) |
| `NLog_StructuredPayload` | idem |
| `LoggerHelper_StructuredPayload` | idem |
| `RawSerilog_BelowMinLevel` | Debug filtrato — caso prod comune |
| `NLog_BelowMinLevel` | idem |
| `LoggerHelper_BelowMinLevel` | idem |

### RoutingBenchmark
Differenziatore unico di LoggerHelper: routing per livello verso sink diversi.

| Benchmark | Cosa misura |
|---|---|
| `Serilog_SingleSink` | Serilog, 1 sink — **baseline** |
| `NLog_SingleTarget` | NLog, 1 target |
| `LoggerHelper_SingleRoute` | LoggerHelper, 1 route |
| `LoggerHelper_MultiRoute_Info` | Console(Info,Warning,Error)+File(Error,Fatal) — messaggio Info va a 1 sink |
| `LoggerHelper_MultiRoute_Error` | stesso setup — messaggio Error instradato a 2 sink (Console + File) |

### StartupBenchmark
Costo di inizializzazione — rilevante per lambda/cold start.

| Benchmark | Cosa misura |
|---|---|
| `RawSerilog_Startup` | `new LoggerConfiguration().CreateLogger()` — **baseline** |
| `NLog_Startup` | `LogManager.Setup().LoadConfiguration(...)` |
| `LoggerHelper_Startup` | `AddLoggerHelper(...)` + `BuildServiceProvider()` + risoluzione `ILoggerProvider` |

---

## Fix al .csproj esistente

Il file attuale contiene `c:` prima di `<PackageReference Include="BenchmarkDotNet">` —
errore di sintassi XML che impedisce il build. Va rimosso nel rewrite.

Dipendenze da aggiungere:
- `NLog` — logger competitor
- `NLog.Extensions.Logging` — per setup MEL-compatibile se necessario
- `BenchmarkDotNet` — già presente (solo fix sintassi)

---

## Automazione GitHub Actions

**File:** `.github/workflows/benchmarks.yml`

**Trigger:**
- Push di tag `v*` (release automatica)
- `workflow_dispatch` (esecuzione manuale on-demand)

**Job flow:**
```
1. checkout + setup dotnet (versione: net9.0 — LTS, stabile per CI)
2. dotnet build -c Release (solo progetto benchmark)
3. dotnet run -c Release -- --filter * --exporters json markdown
4. Script bash: parse output BenchmarkDotNet → genera docs/benchmarks.md
5. git commit + push docs/benchmarks.md su main
```

**Note importanti:**
- Build deve essere `Release` — BenchmarkDotNet rifiuta `Debug`
- Il job benchmark è **separato** da `publish.yml` — non blocca il publish in caso di regressione
- I risultati vengono committati su `main` direttamente dal workflow (richiede `GITHUB_TOKEN` con write permission)

---

## Output pubblicato

### `docs/benchmarks.md`

```markdown
# LoggerHelper v5 — Benchmark Results
> Generated: {date} | Runtime: .NET 9 | OS: ubuntu-latest | BenchmarkDotNet {version}

## Throughput
| Method | Mean | Error | StdDev | Ratio | Allocated |
| ------ | ---- | ----- | ------ | ----- | --------- |
...

## Routing Overhead
...

## Startup Time
...
```

### README NuGet (sezione aggiunta)

```markdown
## Performance

LoggerHelper v5 adds ~X ns overhead over raw Serilog per message.
Multi-sink routing costs <Y ns regardless of sink count.

→ [Full benchmark results](docs/benchmarks.md)
```

---

## Criteri di successo

- Il progetto compila in `Release` senza errori
- I benchmark producono numeri stabili (StdDev < 5% della Mean)
- LoggerHelper risulta entro **2x** il throughput di Serilog raw (overhead accettabile per lo strato di astrazione)
- `docs/benchmarks.md` viene generato e committato correttamente dal workflow CI
- Il README riporta un riassunto leggibile con link alla pagina dettaglio

---

## Fuori scope

- MEL puro (senza Serilog o NLog sotto) — meno significativo per il confronto
- Benchmark per configurazione JSON vs fluent — potenziale Fase 3 (sito interattivo)
- Benchmark per sink I/O reale (file, DB) — misurerebbe il sink, non LoggerHelper
- HTML export per sito — Fase 3
