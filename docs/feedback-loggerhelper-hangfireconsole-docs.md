# Feedback Documentazione CSharpEssentials.LoggerHelper.Sink.HangfireConsole

Data analisi: 2026-05-31

## README GitHub — Cosa manca

### 1. Esempio end-to-end completo
Il README mostra frammenti isolati. Manca un `Program.cs` completo che mostri:
- `AddHangfireConsoleSink()`
- `AddLoggerHelper(configuration)`
- `AddHangfire(config => config.UseConsole())`
- Job class con `Set(context)` / `Clear()`

### 2. Prerequisito Hangfire.Console non menzionato
Non dice esplicitamente che serve:
- NuGet `Hangfire.Console`
- `configuration.UseConsole()` nella configurazione Hangfire

Senza `.UseConsole()` il sink non fa nulla e non c'e' nessun errore — silenzioso.

### 3. Nessun troubleshooting
Domande comuni senza risposta:
- Cosa succede se dimentichi `accessor.Clear()`? (log leakano nel job successivo sullo stesso thread via AsyncLocal)
- Cosa succede fuori da un job? (log ignorati silenziosamente — OK ma va detto)
- Serve in un progetto WebApi che non esegue job? (No, solo nel worker)

### 4. Target framework non specificato
Aggiungere tabella supporto: net8.0, net9.0, net10.0

### 5. Manca esempio con ILogger<T>
Il punto chiave del sink e' che `_logger.LogInformation(...)` dentro un job appare automaticamente sulla dashboard Hangfire. Questo va mostrato esplicitamente — e' il valore aggiunto rispetto a `performContext.WriteLine()`.

## loggerhelper.com — Cosa manca

### 1. Sezione HangfireConsole troppo breve
Solo ~3 righe vs gli altri sink che hanno esempi completi. Serve almeno lo stesso livello di dettaglio del README.

### 2. Compatibilita' formato legacy
Il sito non menziona che il formato `Serilog:SerilogConfiguration` (v2/v4) viene auto-mappato quando `LoggerHelper:Routes` e' vuoto. Informazione utile per chi migra.
