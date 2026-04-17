# LoggerHelper v5 — Miglioramenti rispetto alla versione originale

> Documento di riferimento interno. Usarlo come base per marketing, README NuGet,
> blog post, e confronti con i competitor.

---

## Indice

1. [Quadro generale](#1-quadro-generale)
2. [Compatibilità ILogger\<T\>](#2-compatibilità-iloggert)
3. [BeginScope — properties contestuali](#3-beginscope--properties-contestuali)
4. [API di registrazione — tutti gli overload](#4-api-di-registrazione--tutti-gli-overload)
5. [Architettura — SOLID vs originale](#5-architettura--solid-vs-originale)
6. [Diagnostica degli errori](#6-diagnostica-degli-errori)
7. [Configurazione JSON semplificata](#7-configurazione-json-semplificata)
8. [Tabella riepilogativa](#8-tabella-riepilogativa)

---

## 1. Quadro generale

| | Versione originale (root) | v5 (src/) |
|---|---|---|
| API principale | `loggerExtension<T>` statica | `ILogger<T>` nativo MEL |
| Configurazione | `SerilogCondition[]` + `SerilogOption` complessa | `LoggerHelperOptions` + fluent builder |
| Compatibilità | Solo Serilog diretto | MEL + Serilog + OpenTelemetry |
| Registrazione DI | `AddLoggerHelper()` solo su `IServiceCollection` | Su `IServiceCollection` E `ILoggingBuilder` |
| BeginScope | Non implementato (restituiva null) | Funzionante, propaga a Serilog |
| Architettura | Dipendenze concrete e statiche | Interfacce + DIP |
| Diagnostica errori | Store statico | `ILogErrorStore` iniettabile e testabile |
| Plugin sink | Registry statico | `ISinkPluginRegistry` astraibile |

---

## 2. Compatibilità ILogger\<T\>

### Il problema nell'originale

L'originale richiedeva di usare la classe statica proprietaria `loggerExtension<T>`:

```csharp
// ORIGINALE — accoppiamento forte, non standard
loggerExtension<OrderService>.Log(LogEventLevel.Information, request, null, "Ordine creato");
```

Chi aveva già `ILogger<T>` doveva cambiare **tutto** il codice esistente per adottare LoggerHelper.

### La soluzione in v5

v5 implementa `ILoggerProvider` e `ILogger` di `Microsoft.Extensions.Logging`.
Chi usa già `ILogger<T>` non cambia **nemmeno una riga** di codice applicativo:

```csharp
// v5 — codice applicativo invariato, i log vanno automaticamente su tutti i sink configurati
public class OrderService(ILogger<OrderService> logger) {
    public void ProcessOrder(int orderId) {
        logger.LogInformation("Ordine {OrderId} ricevuto", orderId);   // → Console, File, Email...
        logger.LogError("Pagamento fallito per ordine {OrderId}", orderId); // → solo Email se configurato
    }
}
```

### Come funziona internamente

```
ILogger<T>.LogInformation(...)
    └─► LoggerHelperLogger.Log(...)           // bridge MEL → Serilog
        └─► Serilog.ILogger.Write(...)        // pipeline Serilog
            ├─► Console sink  (se Info configurato)
            ├─► File sink     (se Info configurato)
            └─► Email sink    (se Error configurato)
```

I livelli MEL vengono mappati a Serilog automaticamente:

| MEL | Serilog |
|---|---|
| `Trace` | `Verbose` |
| `Debug` | `Debug` |
| `Information` | `Information` |
| `Warning` | `Warning` |
| `Error` | `Error` |
| `Critical` | `Fatal` |

---

## 3. BeginScope — properties contestuali

### Cosa è BeginScope

`BeginScope` è il meccanismo di MEL per associare **properties di business** a un
blocco di log, senza riscriverle ad ogni riga.

### Properties automatiche vs properties con BeginScope

| Tipo | Chi le aggiunge | Esempi |
|---|---|---|
| Framework | ASP.NET Core automaticamente | `RequestId`, `RequestPath`, `TraceId` |
| LoggerHelper | Middleware e enricher | `ApplicationName`, `MachineName`, `RenderedMessage` |
| Business (tue) | Tu, esplicitamente con `BeginScope` | `OrderId`, `UserId`, `TenantId`, `PaymentProvider` |

### Il problema nell'originale

`BeginScope` restituiva `null`. Le properties venivano silenziosamente ignorate.

### La soluzione in v5

`BeginScope` chiama `LogContext.PushProperty` per ogni property. Serilog le allega
automaticamente a ogni log emesso dentro il blocco `using`:

```csharp
// Caso d'uso tipico: correlare tutti i log di un'operazione di business
using (_logger.BeginScope(new Dictionary<string, object?> {
    ["OrderId"] = orderId,
    ["UserId"] = userId
}))
{
    _logger.LogInformation("Elaborazione avviata");
    // output → {"Message":"Elaborazione avviata", "OrderId":123, "UserId":"mario", ...}

    ValidaOrdine();
    // anche i log dentro ValidaOrdine portano OrderId e UserId

    _logger.LogInformation("Elaborazione completata");
    // output → {"Message":"Elaborazione completata", "OrderId":123, "UserId":"mario", ...}
}
// qui fuori: OrderId e UserId non ci sono più
```

### Scope annidati

Gli scope si accumulano. Il log più interno porta le properties di tutti gli scope aperti:

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = 123 }))
{
    _logger.LogInformation("Avvio ordine");
    // → OrderId=123

    using (_logger.BeginScope(new Dictionary<string, object?> { ["PaymentProvider"] = "Stripe", ["Amount"] = 99.90 }))
    {
        _logger.LogInformation("Chiamata pagamento");
        // → OrderId=123, PaymentProvider="Stripe", Amount=99.90

        _logger.LogWarning("Tentativo 2/3 — provider lento");
        // → OrderId=123, PaymentProvider="Stripe", Amount=99.90
    }

    _logger.LogInformation("Ordine completato");
    // → OrderId=123   (scope pagamento chiuso, PaymentProvider sparita)
}
```

### Propagazione nei metodi chiamati

Le properties di scope si propagano automaticamente a qualsiasi metodo chiamato dentro il blocco:

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = orderId }))
{
    ValidaStock();   // i log dentro hanno OrderId
    CalcolaTasse();  // i log dentro hanno OrderId
    NotificaCliente(); // i log dentro hanno OrderId
}

// Non devi passare orderId a nessuno di questi metodi per averlo nei log
void ValidaStock() {
    _logger.LogInformation("Verifica magazzino"); // → OrderId c'è già
}
```

---

## 4. API di registrazione — tutti gli overload

### Su IServiceCollection (3 overload)

#### Overload 1 — Solo fluent

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
    .AddRoute("Email",   LogEventLevel.Error, LogEventLevel.Fatal)
);
```

**Quando usarlo:** progetto nuovo, vuoi configurare tutto via codice, preferisci
Intellisense e refactoring-safe al JSON.

---

#### Overload 2 — Solo JSON

```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
```

Con `appsettings.LoggerHelper.json`:
```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning"] },
      { "Sink": "Email",   "Levels": ["Error", "Fatal"] }
    ]
  }
}
```

**Quando usarlo:** vuoi cambiare la configurazione **senza ricompilare** (es. production
vs staging), team ops che gestisce il JSON separatamente dallo sviluppatore.
LoggerHelper carica automaticamente `appsettings.LoggerHelper.debug.json` in Development
e `appsettings.LoggerHelper.json` negli altri ambienti.

---

#### Overload 3 — JSON + fluent (merge)

```csharp
builder.Services.AddLoggerHelper(builder.Configuration, b => b
    .AddRoute("Console", LogEventLevel.Debug)   // aggiunta via codice
    // le route del JSON vengono mantenute, questa si aggiunge
);
```

**Quando usarlo:** hai una base JSON condivisa tra ambienti, ma in Development vuoi
aggiungere sink extra (es. Console a Debug) solo via codice senza modificare il JSON.
Le route fluent sono **additive**: non sovrascrivono il JSON, si sommano.

---

### Su ILoggingBuilder (2 overload — novità v5)

La differenza chiave rispetto a `IServiceCollection`: i filtri `"Logging:LogLevel"`
di `appsettings.json` vengono **rispettati** perché il provider passa attraverso
il sistema MEL ufficiale.

#### Overload 4 — ILoggingBuilder + JSON

```csharp
builder.Logging.ClearProviders();  // rimuove Console/Debug/EventLog di default
builder.Logging.AddLoggerHelper(builder.Configuration);
```

Con `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  }
}
```

**Quando usarlo:** vuoi che LoggerHelper sia **l'unico** provider di log e che
rispetti i filtri MEL standard. Ideale per chi già usa `"Logging:LogLevel"` in
configurazione e non vuole cambiare abitudini.

---

#### Overload 5 — ILoggingBuilder + fluent

```csharp
builder.Logging.ClearProviders();
builder.Logging.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error)
    .EnableRequestResponseLogging()
);
```

**Quando usarlo:** stesso scenario del precedente ma preferisci la configurazione
fluent. Tipico per microservizi Docker dove tutto è in codice e il JSON è minimale.

---

### Tabella decisionale — quale overload scegliere

| Scenario | Overload consigliato |
|---|---|
| Progetto nuovo, config in codice | Overload 1 (`Services` + fluent) |
| Config esternalizzata, ops gestisce JSON | Overload 2 (`Services` + JSON) |
| Base JSON + extra in Development | Overload 3 (`Services` + JSON + fluent) |
| LoggerHelper come unico logger, rispetta filtri MEL | Overload 4 (`Logging` + JSON) |
| Microservizio Docker, tutto in codice | Overload 5 (`Logging` + fluent) |

---

## 5. Architettura — SOLID vs originale

### Originale: dipendenze concrete e statiche

```csharp
// ORIGINALE — viola DIP: dipende da SinkPluginRegistry concreto e statico
SinkPluginRegistry.Instance.Register(plugin);
loggerExtension<T>.Log(...);   // classe statica, impossibile da mockare nei test
```

### v5: interfacce + Dependency Inversion

```csharp
// v5 — dipende da interfacce, iniettabili e mockabili
public sealed class SinkRoutingEngine(
    LoggerHelperOptions options,
    ILogErrorStore errorStore,        // interfaccia
    ISinkPluginRegistry registry,     // interfaccia
    IPluginDiscovery pluginDiscovery  // interfaccia
) { ... }
```

### Open/Closed Principle sui sink

Il core non conosce i nomi dei sink. Ogni sink package si auto-registra via
`ModuleInitializer`. Aggiungere un nuovo sink = aggiungere un NuGet, zero modifiche al core:

```csharp
// Nel pacchetto CSharpEssentials.LoggerHelper.Sink.Email
[ModuleInitializer]
internal static void Register() =>
    SinkPluginRegistry.Instance.Register(new EmailSinkPlugin());
```

---

## 6. Diagnostica degli errori

### Originale

Store statico, non iniettabile, non testabile.

### v5

`ILogErrorStore` è registrato come singleton nel DI. Se un sink fallisce durante
la configurazione (es. stringa di connessione sbagliata), l'errore viene catturato
e reso disponibile senza crashare l'applicazione:

```csharp
// Inietta ILogErrorStore per vedere se qualche sink ha fallito all'avvio
app.MapGet("/health/logging", (ILogErrorStore errors) =>
    errors.Count == 0
        ? Results.Ok("All sinks OK")
        : Results.Problem(string.Join(", ", errors.GetAll().Select(e => e.ErrorMessage)))
);
```

---

## 7. Configurazione JSON semplificata

### Originale — modello complesso

```json
{
  "Serilog": {
    "SerilogCondition": [
      {
        "Name": "Console",
        "LogEventLevels": ["Information", "Warning"],
        "SerilogOption": { ... }
      }
    ]
  }
}
```

### v5 — modello leggibile

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "General": {
      "EnableRequestResponseLogging": true
    },
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning"] },
      { "Sink": "Email",   "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "Email": {
        "To": "ops@example.com",
        "From": "noreply@example.com"
      }
    }
  }
}
```

---

## 8. Tabella riepilogativa

| Feature | Originale | v5 |
|---|---|---|
| `ILogger<T>` nativo | ✗ | ✅ |
| `BeginScope` funzionante | ✗ | ✅ |
| `ILoggingBuilder` integration | ✗ | ✅ |
| Filtri `Logging:LogLevel` rispettati | ✗ | ✅ |
| API fluent | Parziale | ✅ Completa |
| Configurazione JSON | ✅ Complessa | ✅ Semplificata |
| JSON + fluent merge | ✗ | ✅ |
| `ILogErrorStore` iniettabile | ✗ | ✅ |
| Architettura SOLID | Parziale | ✅ |
| OpenTelemetry bridge | Presente | ✅ Migliorato |
| Sink auto-registrazione | ✅ | ✅ |
| Propagazione scope ai metodi | ✗ | ✅ |
| Scope annidati | ✗ | ✅ |
