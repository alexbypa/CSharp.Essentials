# LoggerHelper v5 — Dalla vecchia versione a `src/`  
### *Tutto quello che conta, in un colpo d’occhio — e come provarlo sul serio*

[![.NET](https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-purple)](https://dotnet.microsoft.com/download)

> **Riferimento storico (funzionalità sink documentate lì):**  
> [README — Csharp.Essentials.Extensions](https://github.com/alexbypa/Csharp.Essentials.Extensions/blob/main/README.md)  
> Quel repo resta la “bibbia” delle configurazioni legacy (`Serilog:SerilogConfiguration`, `SerilogCondition`, `TelegramOption`, Email HTML, ecc.). **LoggerHelper v5 in `src/`** riporta lo stesso spirito: stessi scenari, schema più pulito e migliore integrazione con `ILogger<T>`.

---

## Hai tutto nella cartella `src/` come nella vecchia versione?

**In pratica: sì, per il nucleo di prodotto che interessa a chi usava Extensions + i sink “classici”.**  
La linea **ufficiale** e **da pubblicare su NuGet** è sotto `src/`. La cartella `CSharpEssentials.LoggerHelper/` in **root del repo** è la traccia **legacy / congelata**; non è l’obiettivo del rollout v5.

| Cosa cercavi nella vecchia doc (Extensions README) | In `src/` v5 |
|------------------------------------------------------|----------------|
| Routing per livello (prima `SerilogCondition`) | `LoggerHelper:Routes` — **più semplice**; in più **adapter automatico** da `Serilog:SerilogConfiguration` se non hai ancora migrato il JSON |
| **Console** sink | `CSharpEssentials.LoggerHelper.Sink.Console` |
| **File** sink | `CSharpEssentials.LoggerHelper.Sink.File` |
| **Email** (SMTP, HTML, throttle) | `...Sink.Email` — parità funzionale con la doc legacy |
| **Telegram** (`TelegramOption`, bot, throttle) | `...Sink.Telegram` — chiavi legacy supportate |
| **PostgreSQL** (colonne custom, JSONB, ecc.) | `...Sink.Postgresql` — `PostgreSQL` / `PostgreSql` come nome route |
| **SQL Server** (+ colonne aggiuntive, opzioni nested) | `...Sink.MSSqlServer` |
| **Elasticsearch** | `...Sink.Elasticsearch` |
| **Seq** | `...Sink.Seq` *(tipico stack Serilog; nella tabella Extensions sopra non sempre in evidenza, ma in v5 è un pacchetto first-class)* |
| **HttpHelper** (client nominati, rate limit, mock…) | `src/CSharpEssentials.HttpHelper/` + test |
| **xUnit** sink | *Non ancora in `src/` v5* — in roadmap (come in README parity) |
| **Telemetry / AI / Dashboard** | *Non in scope v5 core* — come da piano; restano pacchetti separati / futuri |

📌 **Dettaglio tecnico parity:** vedi anche [`legacy-parity-v5.md`](legacy-parity-v5.md).

**In una frase:** se ti servivano **LoggerHelper + sink operativi + HttpHelper**, in `src/` hai **coverage completo** rispetto a ciò che la vecchia README descrive per quei blocchi. Ciò che **Extensions** pubblicizza come Satellite **xUnit / Telemetry / AI / Dashboard** non è nella stessa cartella come pacchetti v5 rinnovati — per scelta di scope.

---

## Mappa veloce `src/` (orientamento)

```
src/
├── CSharpEssentials.LoggerHelper/              ← core orchestrazione + adapter legacy JSON
├── CSharpEssentials.LoggerHelper.SourceGenerator/
├── CSharpEssentials.LoggerHelper.Sink.{Console|File|Email|Telegram|Postgresql|MSSqlServer|Elasticsearch|Seq}
├── CSharpEssentials.HttpHelper/ (+ .Tests)
├── CSharpEssentials.LoggerHelper.TestApp/      ← mini API per giocare con Swagger + log
├── CSharpEssentials.LoggerHelper.Demo/         ← routing, trace, diagnostica
├── CSharpEssentials.LoggerHelper.Tests/
└── CSharpEssentials.LoggerHelper.Benchmarks/
```

---

## Come provare le funzionalità (passo dopo passo)

### 1. Test automatici — “sanity check” in 30 secondi

Dalla root del repo (Windows PowerShell):

```powershell
cd d:\Project_Pixelo\CSharp.Essentials
dotnet test src\CSharpEssentials.LoggerHelper.Tests\CSharpEssentials.LoggerHelper.Tests.csproj -c Release
```

✅ Ci si aspetta **tutti i test verdi** (routing, `BeginScope`, provider `ILogger<T>`, adapter legacy, integrazione host, ecc.).

Per **HttpHelper**:

```powershell
dotnet test src\CSharpEssentials.HttpHelper.Tests\CSharpEssentials.HttpHelper.Tests.csproj -c Release
```

---

### 2. TestApp + Swagger — *toccare con mano `ILogger<T>`*

```powershell
cd d:\Project_Pixelo\CSharp.Essentials\src\CSharpEssentials.LoggerHelper.TestApp
dotnet run
```

- Apri **`/`** nella UI di Swagger se è configurato come nell’originale progetto (*spesso Swagger è sulla radice dell’host*).
- Chiama **`GET /api`** — log Information / Warning / Error in console (secondo `appsettings.LoggerHelper.json`).
- Prova **`GET /api/orders/{orderId}`** e **`/api/orders/{orderId}/pay`** — **scope** e **scope annidati** (properties su ogni log).
- **`GET /api/diagnostics/`** — vedi route caricate, plugin registrati, errori sink.

Per cambiare sink di prova senza ricompilare tutto il mondo: edita **`appsettings.LoggerHelper.json`**, aggiungi il **pacchetto NuGet del sink**, la sezione sotto **`Sinks`** e una **route** verso quel nome sink.

---

### 3. Demo “full fat” (`LoggerHelper.Demo`)

```powershell
cd d:\Project_Pixelo\CSharp.Essentials\src\CSharpEssentials.LoggerHelper.Demo
dotnet run
```

**Qui** trovi **routing demo**, **Trace/BeginTrace**, proprietà custom, **`RequestResponseLoggingMiddleware`**, diagnostica — ideale per avvicinarsi alla complessità reale dopo la TestApp.

---

### 4. Sink “vere” (Email, DB, Telegram, …)

Il README Extensions mostra blocchi JSON sotto **`Serilog`**. Con v5 puoi ancora usarli: il core **rileva** `Serilog:SerilogConfiguration`. In alternativa (consigliato):

1. **`dotnet add package`** sul sink che ti serve (`...Sink.Email`, ecc.).
2. Aggiungi in **`appsettings.LoggerHelper.json`** (o nome env-specifico della doc core) **`Routes`** + sezione **`Sinks`** omologa alla vecchia **`SerilogOption`**.

📖 Esempi di nomi proprietà legacy (Email throttle, Telegram, colonne MSSQL/PG…) sono ancora utili — confronta paragrafo per paragrafo con il README Extensions e mappa sulla sezione `LoggerHelper:Sinks` del README in `src/CSharpEssentials.LoggerHelper/README.md`.

---

### 5. Template da zero (`dotnet new`)

```powershell
dotnet new install d:\Project_Pixelo\CSharp.Essentials\templates\loggerhelper-api
dotnet new loggerhelper-api -n MiaApiLogs
cd MiaApiLogs
dotnet run
```

Ottimo per vedere il **minimal setup** pulito senza rumor del resto della soluzione.

---

### 6. Sito promo + playground *(nessun backend reale ai sink)*

Apri **`docs/site/index.html`** nel browser — oppure servi la cartella `docs/site/` con qualsiasi web server statico.  
Utile per **UX**, tabella package, snippet; **non** sostituisce una prova con sink Email/DB veri.

---

## Tabella riassuntiva “Extensions README → comando / file v5”

| Dal README Extensions | Dove lo provo in questo repo |
|------------------------|------------------------------|
| HttpHelper + configurazione client | `CSharpEssentials.HttpHelper` + relativi `.Tests`; integrazione reale sulla tua API |
| Logger “zero change” `ILogger<T>` | `LoggerHelper.TestApp` → Swagger |
| `SerilogCondition` / opzioni sink | Ancora caricabili via adapter; preferibile `LoggerHelper` JSON + [`README.md` nel core](../src/CSharpEssentials.LoggerHelper/README.md) |
| Email / Telegram / DB sinks | Aggiungi pacchetto sink + `Routes` + `Sinks`; poi `dotnet run` su TestApp o Demo |

---

## Chiusura

- **`src/` = nuova casa** dei pacchetti che vuoi pubblicare e mantenere.  
- **Parità sink** documentata nell’Extensions README: **coperta** per i sink operational elencati in tabella sopra; **satelliti** (xUnit / Telemetry / AI / Dashboard) **fuori dal taglio v5** finché non li portiamo allo stesso modello plugin.  
- **Prova reale veloce:** `dotnet test` + **`LoggerHelper.TestApp`** + file JSON + pacchetti sink che ti servono.

*Buon divertimento — e se vuoi una sola checklist: TestApp ➜ `/api/diagnostics/` ➜ console piena di roba sensata sei a posto.* ✨
