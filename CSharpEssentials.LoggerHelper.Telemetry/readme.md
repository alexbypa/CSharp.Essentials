# IServiceCollection.AddLoggerTelemetry

Questo pacchetto di estensione per `IServiceCollection` fornisce una configurazione centralizzata per la telemetria, il logging e il monitoraggio per le applicazioni .NET Core, integrando Serilog, OpenTelemetry e altre funzionalità correlate al logging e alla metrica.

## Installazione

Aggiungi il pacchetto tramite NuGet:

```bash
dotnet add package IServiceCollection.AddLoggerTelemetry # (sostituire con il nome effettivo del pacchetto)
```

## Utilizzo

Per integrare la telemetria nella tua applicazione, chiama il metodo `AddLoggerTelemetry` all'interno del metodo `ConfigureServices` del tuo `Startup.cs` o del `Program.cs` (per .NET 6+):

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... altre configurazioni
    services.AddLoggerTelemetry(Configuration, Builder); // Assicurati di passare Configuration e Builder
    // ...
}
```

### Configurazione

La configurazione della telemetria è gestita tramite il file `appsettings.json` (e `appsettings.debug.json` per il debug). Ecco un esempio delle sezioni chiave che puoi configurare:

```json
{
  "SerilogConfiguration": {
    "LoggerTelemetryOptions": {
      "IsEnabled": true,
      "ConnectionString": "Server=my_db_server;Database=TelemetryDb;Trusted_Connection=True;MultipleActiveResultSets=true",
      "MeterListenerIsEnabled": true
    }
  },
  // ... altre configurazioni
}
```

* **`SerilogConfiguration:LoggerTelemetryOptions:IsEnabled`**: Abilita o disabilita l'intera funzionalità di telemetria.
* **`SerilogConfiguration:LoggerTelemetryOptions:ConnectionString`**: Stringa di connessione per il database di telemetria (utilizzato da `TelemeteriesDbContext`).
* **`SerilogConfiguration:LoggerTelemetryOptions:MeterListenerIsEnabled`**: Abilita o disabilita il listener di metriche OpenTelemetry.

### Funzionalità incluse

Il metodo `AddLoggerTelemetry` configura le seguenti funzionalità:

* **Serilog**: Configurazione del logging tramite Serilog. La configurazione viene caricata da `appsettings.json` e `appsettings.debug.json` (in modalità debug).
* **DbContext per Telemetria (`TelemeteriesDbContext`)**:
    * Registra un `DbContext` dedicato alla persistenza dei dati di telemetria.
    * Esegue automaticamente le migrazioni del database all'avvio dell'applicazione.
* **Metriche Personalizzate**:
    * Inizializza una classe `CustomMetrics` (presumibilmente per la definizione di metriche specifiche dell'applicazione).
    * Supporto per l'aggiunta di un `OpenTelemetryMeterListenerService` se `MeterListenerIsEnabled` è abilitato.
* **Filtri MVC**:
    * Aggiunge `SingletonStartupFilter` e `TracedPropagationStartupFilter` (presumibilmente per la gestione del ciclo di vita e la propagazione del tracing).
* **OpenTelemetry**:
    * Configurazione di base di OpenTelemetry con `AddOpenTelemetry()`.
    * Integrazione con `WithMetrics()` e `AddAspNetCoreInstrumentation()`.

### Estensioni e Personalizzazioni

* **`TelemeteriesDbContext`**: Il `DbContext` per la telemetria è configurato per ignorare gli avvisi di modifiche relazionali pendenti (`ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))`). Se necessario, questa configurazione può essere rivista per una gestione più rigorosa delle migrazioni.
* **`CustomMetrics`**: Questa classe è il punto in cui puoi definire e strumentare le tue metriche personalizzate. Assicurati che sia implementata per esporre le metriche rilevanti per la tua applicazione.
* **Filtri di Avvio**: I filtri `SingletonStartupFilter` e `TracedPropagationStartupFilter` sono inclusi. Se la tua applicazione richiede logiche di avvio o di propagazione del contesto di tracing più complesse, questi filtri potrebbero essere personalizzati o estesi.

## Contribuzione

Le contribuzioni sono benvenute! Se hai suggerimenti o miglioramenti, sentiti libero di aprire una issue o una pull request.

## Licenza

Questo progetto è rilasciato sotto la licenza [SPECIFICARE LICENZA, es. MIT License].
```