# Analisi Competitiva — Logging .NET (Maggio 2026)

## Panorama del Mercato

| Package | Download Totali | Posizionamento |
|---|---|---|
| Serilog (core) | 2.6B+ | Standard de facto per structured logging |
| Serilog.AspNetCore | 672M+ | Entry point ASP.NET Core |
| NLog | 538M+ | Alternativa storica, forte in XML |
| **CSharpEssentials.LoggerHelper v5** | Crescita target | **Serilog orchestrator** — routing per livello via JSON |

## Gap nel Mercato

Nessun pacchetto offre routing dichiarativo per-livello verso sink multipli con setup JSON minimale + `ILogger<T>` nativo + sink modulari NuGet.

## Posizionamento v5 (shipped in `src/`)

> **"The easiest way to orchestrate Serilog sinks — route logs by level with zero code, just JSON"**

### v5 — Implementato

- `ILogger<T>` nativo (MEL provider)
- `ILoggingBuilder` integration + filtri `Logging:LogLevel`
- `BeginScope` con propagazione Serilog
- Config JSON `LoggerHelper` semplificata + fluent merge
- `ILogErrorStore` + `ILoadedSinkStore` diagnostici
- 8 sink modulari NuGet
- Source generator per registrazione compile-time (AOT-friendly)
- Benchmark pubblicati in [`benchmarks.md`](benchmarks.md)

### In roadmap marketing

- Playground live (Tier 2 WASM)
- Dashboard sink package

## Migrazione v2 → v5

| Legacy | v5 |
|---|---|
| `Serilog:SerilogConfiguration` | `LoggerHelper` |
| `SerilogCondition` | `Routes` |
| `Level` | `Levels` |
| `SerilogOption` | `Sinks` |

## Matrice Feature

| Feature | LoggerHelper v5 | Serilog Puro | NLog |
|---|---|---|---|
| Per-level sink routing via JSON | **Nativo** | sub-logger / expressions | FilteringTargetWrapper |
| `ILogger<T>` senza cambiare codice | **Sì** | Via bridge | Sì |
| Sink modulari NuGet | **Sì** | Sì (config manuale) | Sì |
| Source generator sink registration | **Sì** | No | No |
| OpenTelemetry trace correlation | **Sì** | Parziale | Via extension |

## Fonti

- https://www.nuget.org/packages/serilog/
- https://www.nuget.org/packages/NLog/
- https://www.nuget.org/packages/CSharpEssentials.LoggerHelper/
