# Backlog / TODO

> Spostato da CLAUDE.md il 2026-09-30 · riorganizzato il 2026-10-02.
>
> **Legenda:** `[ ]` da fare · `[/]` in corso / parziale · `[x]` completato

## In corso

- [/] Pipeline autonoma per chiudere ogni task: plugin `microtask-pipeline@ai-dev-arsenal` (agent `solid-analyst` `implementer` `test-runner` `reviewer` `memory-updater` `doc-sync-reviewer`) + skill `/release` `/new-sink`. Da ottimizzare.

## Queue

Eseguire con `/microtask-pipeline:microtask`, un task alla volta, commit a fine gruppo. Sorgente: [optimization-plan](docs/marketing/optimization-plan.md). Task completati → [DONE.md](DONE.md).

| Stato | ID | Gruppo | Tipo | Task |
|---|---|---|---|---|
| [ ] | D7 | G8 | docs | GIF dashboard + MCP nel README NuGet. **Bloccato:** serve registrare le GIF (Demo app + dashboard + chat MCP) e committarle in `img/`; i link raw GitHub darebbero 404 su NuGet finché il file non è su `main`. Script di scena in `outcomes/content/D4.md` |
| [ ] | A39 | G0b | code | Parità repo pubblico `alexbypa/CSharp.Essentials`: portare fix A38 (`ReplaceLineEndings(" ")` nei due `SanitizeLogValue` + test) per mantenere identiche le due copie |
| [ ] | A40 | G0b | analysis | `Directory.Packages.props` non applicato (nessun `ManagePackageVersionsCentrally`, versioni inline nei csproj e divergenti, es. M.E.Configuration 9.0.5 vs 10.0.9): attivare CPM o rimuovere il file e correggere CLAUDE.md |
| [ ] | A41 | G0b | code | Core csproj: allineare major delle dipendenze M.E.* (Configuration/Binder 10.0.9 vs Json/Logging 9.0.1) |
| [ ] | A48 | G6b | code | `HttpHelperLog` (`src/CSharpEssentials.HttpHelper/HttpHelperLog.cs:10`) scrive sul `Serilog.Log` statico, ma `AddLoggerHelper` non assegna mai `Log.Logger`: i log interni di HttpHelper non raggiungono la pipeline LoggerHelper, contrariamente al README HttpHelper (~riga 82). Collegarlo alla pipeline oppure correggere il README (da C1) |
| [ ] | A50 | G6b | docs | Bassa priorità. Demo: il cursore round-robin del mock è condiviso, quindi la sequenza 502/503/200 vale solo per richieste seriali: annotarlo nel README della Demo oppure rendere il match per-request (da C1) |
| [ ] | A51 | G4c | code | Bassa priorità. `IHttpMockScenario`: aggiungere un default interface member che esponga le factory request-aware (`Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>`), così anche le implementazioni custom ricevono request e token nella factory (oggi solo `HttpMockScenario`; nota di design nel commento ponytail di `HttpMockEngine`). Additivo, nessun breaking change |

## Roadmap (promemoria — NON eseguire)

- [/] **MCP:** nuovi tool `set_log_level`, `search_logs`, `toggle_sink`. *(fatti: SSE transport, prompt `diagnose-logging`)*
- [/] **Dashboard:** metriche performance (events/sec, latency), sink actions (enable/disable), export JSON/CSV. *(fatti: live stream SSE, autenticazione obbligatoria Basic / `AuthorizationPolicy`)*
- [/] **Performance:** test AOT/Trimming (`IsTrimmable=true`), Source Generator per auto-registration sink *(prototipo rotto rimosso in A12)*. *(fatto: benchmark vs Serilog/NLog)*
