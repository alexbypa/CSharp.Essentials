# LoggerHelper v5 — Miglioramenti ILogger<T>

## Contesto

Questi miglioramenti completano il bridge tra `Microsoft.Extensions.Logging` (MEL) e
il pipeline Serilog di LoggerHelper. L'obiettivo: chi usa `ILogger<T>` adotta
LoggerHelper senza cambiare una riga di codice applicativo.

---

## Fix 1 — BeginScope propaga le properties a Serilog

**File:** `src/CSharpEssentials.LoggerHelper/Provider/LoggerHelperLogger.cs`

### Il problema

`BeginScope` restituiva `null`. Le properties contestuali (es. `OrderId`, `UserId`)
venivano silenziosamente ignorate e non comparivano nei log Serilog.

### La soluzione

`BeginScope` chiama `LogContext.PushProperty` per ogni property del dizionario.
Serilog aggiunge automaticamente quelle properties a ogni log emesso dentro il blocco `using`.

### Quando usarlo

Quando vuoi che un gruppo di log condivida le stesse properties **senza passarle
a mano ad ogni riga**:

```csharp
// PRIMA (ripetitivo, dimenticabile)
_logger.LogInformation("Ordine ricevuto. OrderId={OrderId}", orderId);
_logger.LogWarning("Stock basso. OrderId={OrderId}", orderId);
_logger.LogError("Pagamento fallito. OrderId={OrderId}", orderId);

// DOPO (con BeginScope)
using (_logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = orderId }))
{
    _logger.LogInformation("Ordine ricevuto");   // OrderId già presente
    _logger.LogWarning("Stock basso");           // idem
    _logger.LogError("Pagamento fallito");       // idem
}
```

### Properties automatiche vs properties con BeginScope

| Tipo | Chi le aggiunge | Esempio |
|---|---|---|
| Automatiche (framework) | ASP.NET Core | `RequestId`, `RequestPath` |
| Automatiche (LoggerHelper) | Middleware | `ApplicationName`, `MachineName` |
| Business (tue) | Tu, con BeginScope | `OrderId`, `UserId`, `TenantId` |

### Scope annidati

Gli scope si accumulano. Il log più interno porta le properties di tutti gli scope aperti:

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = 123 }))
{
    using (_logger.BeginScope(new Dictionary<string, object?> { ["PaymentProvider"] = "Stripe" }))
    {
        _logger.LogInformation("Pagamento");
        // → ha sia OrderId=123 che PaymentProvider="Stripe"
    }
    _logger.LogInformation("Ordine completato");
    // → ha solo OrderId=123 (scope pagamento chiuso)
}
```

### Propagazione nei metodi chiamati

Le properties dello scope si propagano anche ai metodi invocati dentro il blocco:

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = orderId }))
{
    ProcessaOrdine(); // i log dentro ProcessaOrdine portano OrderId
}

void ProcessaOrdine() {
    _logger.LogInformation("Verifica stock"); // OrderId c'è — non serve passarlo
}
```

---

## Fix 2 — Registrazione via ILoggingBuilder

**File:** `src/CSharpEssentials.LoggerHelper/Extensions/ServiceCollectionExtensions.cs`

### Il problema

Il provider veniva registrato direttamente su `IServiceCollection`, bypassando
`ILoggingBuilder`. Questo ignorava i filtri `"Logging:LogLevel"` di `appsettings.json`.

```json
// Questo NON veniva rispettato prima del fix
"Logging": {
  "LogLevel": {
    "Default": "Warning",
    "Microsoft.AspNetCore": "Error"
  }
}
```

### La soluzione

Due nuovi overload su `ILoggingBuilder`:

```csharp
// Via JSON config
builder.Logging.ClearProviders();
builder.Logging.AddLoggerHelper(builder.Configuration);

// Via fluent API
builder.Logging.ClearProviders();
builder.Logging.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
);
```

### Quando usare ILoggingBuilder vs IServiceCollection

| Scenario | Metodo consigliato |
|---|---|
| Progetto nuovo con LoggerHelper come unico logger | `builder.Logging.AddLoggerHelper(...)` |
| Progetto esistente che aggiunge LoggerHelper | `builder.Services.AddLoggerHelper(...)` |
| Vuoi rispettare i filtri di `appsettings.json` | `builder.Logging.AddLoggerHelper(...)` |
| Vuoi sostituire tutti i logger di default | `builder.Logging.ClearProviders()` poi `AddLoggerHelper` |

---

## Esempi in TestApp

Vedi `Program.cs` per tre esempi progressivi:

| Endpoint | Dimostra |
|---|---|
| `GET /` | Uso base `ILogger<T>` — compatibilità zero-code |
| `GET /orders/{orderId}` | BeginScope con properties di business |
| `GET /orders/{orderId}/pay` | Scope annidati |
