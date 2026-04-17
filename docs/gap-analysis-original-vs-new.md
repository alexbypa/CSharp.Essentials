# Gap Analysis: Originale vs Nuovo (src/)

## Feature portate nel nuovo

| Feature | Originale | Nuovo (src/) | Note |
|---|---|---|---|
| Plugin discovery da filesystem | `LoggerBuilder.AddDynamicSinks()` con reflection | `FileSystemPluginDiscovery` + `[ModuleInitializer]` | Piu' pulito, no reflection manuale |
| Per-level sink routing | `SerilogCondition.Level` in JSON | `SinkRouting.Levels` in JSON | Stessa logica, nome semplificato |
| SelfLog → error store | `SerilogSelfLogBridge.Enable()` → `GlobalLogger.Errors` | `LoggerPipelineFactory` → `ILogErrorStore` | DI-based, thread-safe |
| Throttling (Email/Telegram) | `SinkThrottlingManager` statico | `SinkThrottlingManager` statico | Identico |
| OpenTelemetry bridge | `OpenTelemetryLogEventSink` | `OpenTelemetryLogEventSink` | Identico |
| RenderedMessage enricher | `RenderedMessageEnricher` | `RenderedMessageEnricher` | Identico |
| Request/Response middleware | `RequestResponseLoggingMiddleware` con `loggerExtension<T>` | `RequestResponseLoggingMiddleware` con `ILogger<T>` | Migliore: usa ILogger nativo |
| Env-based JSON config | `appsettings.LoggerHelper.debug.json` / `.json` | Stesso meccanismo | Identico |
| Error store diagnostico | `LoggerErrorStore` | `LogErrorStore` + `ILogErrorStore` | Con interfaccia per DIP |

## Feature MANCANTI nel nuovo (da portare)

### 1. IContextLogEnricher — Custom enrichment per-request
**Originale:** `IContextLogEnricher` permette agli utenti di registrare un enricher custom che arricchisce il logger con dati di contesto (es. tenant ID, user ID, correlation ID).
```csharp
public interface IContextLogEnricher {
    ILogger Enrich(ILogger logger, object? context);
    LoggerConfiguration Enrich(LoggerConfiguration configuration);
}
```
**Stato nel nuovo:** Abbiamo `WithEnrichers(Action<LoggerConfiguration>)` nel builder, ma manca l'enrichment **per-request** (cioe' su ogni log call). Questo e' utile in contesti multi-tenant.
**Azione:** Portare `IContextLogEnricher` come interfaccia opzionale registrabile nel DI.

### 2. ILoggerRequest / IRequest — Structured request context
**Originale:** Interfacce `ILoggerRequest` e `IRequest` con campi `IdTransaction`, `Action`, `ApplicationName`. Usate per arricchire ogni log entry con contesto transazionale.
```csharp
public interface ILoggerRequest {
    string IdTransaction { get; }
    string Action { get; }
    string ApplicationName { get; }
}
```
**Stato nel nuovo:** Non presente. Il nuovo usa `ILogger<T>` nativo che non ha questo concetto built-in.
**Azione:** Rivalutare. Con `ILogger<T>` + `LogContext.PushProperty()` si ottiene lo stesso risultato in modo standard. Documentare il pattern piuttosto che re-implementare l'interfaccia custom.

### 3. InMemoryDashboardSink — Log buffer per dashboard
**Originale:** `InMemoryDashboardSink` mantiene un buffer circolare (5000 log) per la dashboard interattiva. I log del canale "Dashboard" vengono scritti qui.
**Stato nel nuovo:** Non presente.
**Azione:** Portare come sink separato (`CSharpEssentials.LoggerHelper.Dashboard`). E' una feature a valore aggiunto unica.

### 4. LoadedSinkInfo — Diagnostica sink caricati
**Originale:** `LoadedSinkInfo` tiene traccia di quali sink sono stati caricati con successo e per quali livelli.
**Stato nel nuovo:** Non presente. L'error store cattura solo gli errori, non i successi.
**Azione:** Aggiungere `LoadedSinkInfo` al `SinkRoutingEngine` per diagnostica completa.

### 5. ConfigurationPrinter — Debug configurazione
**Originale:** `ConfigurationPrinter.PrintByProvider()` stampa tutte le chiavi/valori di configurazione per ogni provider, utile per debug.
**Stato nel nuovo:** Non presente.
**Azione:** Portare come utility opzionale, attivabile con `EnableSelfLogging`.

### 6. TolerantPluginLoadContext — Load context fault-tolerant
**Originale:** `TolerantPluginLoadContext` gestisce il caricamento degli assembly in modo tollerante — se un'assembly dipendenza manca, ritorna null invece di crashare.
**Stato nel nuovo:** `FileSystemPluginDiscovery` carica nel `Default` context. Meno isolato ma funziona con `[ModuleInitializer]`.
**Azione:** Valutare se serve. Il nuovo approccio con `[ModuleInitializer]` e' piu' semplice e funziona bene. Il `TolerantPluginLoadContext` aggiunge complessita' per un edge case raro.

### 7. PostgreSQL configurable columns
**Originale:** `ColumnsPostGreSQL` nel JSON permette di configurare colonne custom per il sink PostgreSQL.
```json
"ColumnsPostGreSQL": [
  { "Name": "tenant_id", "Writer": "SingleProperty", "Type": "Text", "Property": "TenantId" }
]
```
**Stato nel nuovo:** Il sink PostgreSQL ha colonne hard-coded in `BuildDefaultColumns()`.
**Azione:** Portare la configurabilita' delle colonne via JSON nel sink PostgreSQL.

### 8. MSSqlServer additional columns + column options
**Originale:** Supporta `additionalColumns` e `columnOptionsSection` (add/remove standard columns).
**Stato nel nuovo:** Solo `ConnectionString`, `TableName`, `SchemaName`, `AutoCreateSqlTable`.
**Azione:** Portare `additionalColumns` e `columnOptionsSection` nel sink MSSqlServer.

## Migliorie del nuovo rispetto all'originale

### Gia' implementate
1. **ILogger<T> nativo** — L'originale usa solo `loggerExtension<T>.TraceSync()` statico. Il nuovo supporta `ILogger<T>` standard.
2. **DI-first** — L'originale usa `GlobalLogger` statico + `ServiceLocator`. Il nuovo e' full DI.
3. **SOLID** — Interfacce, SRP, OCP (sink extensibili senza modificare il core).
4. **Thread-safety** — `ConcurrentQueue` invece di `List<T>` con lock manuale.
5. **No reflection manuale** — `[ModuleInitializer]` auto-registra i plugin.

### Proposte di migliorie future
1. **Source Generator** — Sostituire la discovery da filesystem con source generator per AOT/trimming (da CLAUDE.md Fase 1).
2. **Seq ApiKey** — L'originale non supporta ApiKey per Seq, il nuovo si'.
3. **Telegram ThrottleInterval configurabile** — L'originale ha `ThrottleInterval` nelle opzioni Telegram, il nuovo ha throttle hardcoded a 1 secondo.
4. **Email SmtpServer/SmtpPort naming** — L'originale usa `Host`/`Port`, il nuovo anche. Coerente.

## Struttura JSON originale vs nuova

### Originale (appsettings.LoggerHelper.json)
```json
{
  "Serilog": {
    "SerilogConfiguration": {
      "ApplicationName": "MyApp",
      "SerilogCondition": [
        { "Sink": "Console", "Level": ["Information", "Warning"] }
      ],
      "SerilogOption": {
        "File": { "Path": "Logs", ... },
        "Email": { "Host": "smtp...", ... }
      }
    }
  }
}
```

### Nuovo (appsettings.LoggerHelper.json)
```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning"] }
    ],
    "Sinks": {
      "File": { "Path": "Logs", ... },
      "Email": { "Host": "smtp...", ... }
    }
  }
}
```

**Differenze:** Sezione root semplificata (`LoggerHelper` vs `Serilog:SerilogConfiguration`), `SerilogCondition` → `Routes`, `Level` → `Levels`, `SerilogOption` → `Sinks`.
