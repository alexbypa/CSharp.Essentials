# Analisi Competitiva — Logging .NET (Aprile 2026)

## Panorama del Mercato

| Package | Download Totali | Download/Giorno | Posizionamento |
|---|---|---|---|
| Serilog (core) | 2.6B | ~7.7M | Standard de facto per structured logging |
| Serilog.AspNetCore | 672.5M | — | Entry point per ASP.NET Core |
| Serilog.Sinks.Console | 1.0B | — | Sink più scaricato |
| Serilog.Sinks.File | 1.0B | — | Secondo sink più scaricato |
| NLog | 538.7M | ~945K | Alternativa storica, forte in config XML |
| NLog.Extensions.Logging | 213M | — | Bridge con ILogger |
| Microsoft.Extensions.Logging | Built-in | — | Abstraction layer di default |
| log4net | Legacy | In declino | Nessuna structured logging nativa |
| **CSharpEssentials.LoggerHelper** | **5.1K** | **~42** | **Target: 1000/day** |

## Gap nel Mercato

Nessun pacchetto offre routing dichiarativo per-livello verso sink multipli con una semplice config JSON.

### Come funziona oggi (senza LoggerHelper)

**Serilog puro** — Per mandare Error via email e Info su console:
- Configurare sub-logger con `WriteTo.Conditional()`
- Oppure usare `serilog-expressions` (pacchetto separato)
- Oppure scrivere codice C# manuale con `Filter.ByIncludingOnly()`

**NLog** — Regole XML complesse con `FilteringTargetWrapper`

**LoggerHelper** — Una riga JSON per sink e livelli (`SerilogCondition`)

## Posizionamento Strategico

**NON** competere come "alternativa a Serilog". Posizionamento:

> **"The easiest way to orchestrate Serilog sinks — route logs by level with zero code, just JSON"**

LoggerHelper = **Serilog orchestrator/wrapper**, non sostituto.

### Tagline
> "5 righe di JSON. Tutti i sink. Ogni livello dove vuoi."

## Matrice Feature

| Feature | LoggerHelper | Serilog Puro | NLog |
|---|---|---|---|
| Per-level sink routing via JSON | **Nativo** | Richiede sub-logger/expressions | Richiede FilteringTargetWrapper XML |
| Plugin sinks modulari (NuGet separati) | **Sì** | Sì (ma config manuale) | Sì |
| Dashboard integrato | **Sì** | No (serve Seq/$) | No |
| AI log analysis | **Sì** | No | No |
| OpenTelemetry trace correlation | **Sì** | Solo da Serilog 4.x | Via NLog.DiagnosticSource |
| xUnit test sink | **Sì** | Via community sink | Via NLog.Targets.Memory |
| Setup in 5 righe | **Obiettivo rewrite** | ~15-20 righe | ~10-15 righe XML |

## Punti Deboli da Risolvere nel Rewrite

1. **No `ILogger<T>` support** — Esclude chi usa Microsoft.Extensions.Logging
2. **Solo config JSON** — Nessuna fluent API programmatica
3. **Runtime reflection per sink loading** — Slow startup, non AOT-safe
4. **README NuGet** non ottimizzato per conversione
5. **Nessun benchmark** pubblicato
6. **SEO NuGet** — Description troppo lunga, keywords non ottimali

## Fonti
- https://www.nuget.org/packages/serilog/
- https://www.nuget.org/packages/NLog/
- https://www.nuget.org/packages/serilog.aspnetcore
- https://www.nuget.org/packages/CSharpEssentials.LoggerHelper/2.0.9
- https://www.nuget.org/packages/Microsoft.Extensions.Logging/
- https://betterstack.com/community/guides/logging/best-dotnet-logging-libraries/
- https://blog.elmah.io/serilog-vs-nlog/
- https://github.com/serilog/serilog-expressions
