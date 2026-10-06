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
| [ ] | A21 | G9 | code | Coverage in CI sulla repo **pubblica** `alexbypa/CSharp.Essentials` (non questa privata): `build-test.yml` → `--collect "Code Coverage;Format=cobertura"` sui 2 progetti di test, merge con ReportGenerator (solo assembly `CSharpEssentials.*`, esclusi Tests/TestApp/Demo/Benchmarks), riepilogo in `$GITHUB_STEP_SUMMARY`; upload Codecov (`CODECOV_TOKEN`) + **badge coverage** in README root e README pacchetto. Nessuna soglia bloccante finché non c'è una baseline |
| [ ] | A52 | G10 | code | Bug `MySqlColumnMap.IdentifierPattern` (`MySqlColumnMap.cs:26`): `$` accetta `"abc"`; usare `\z`. Aggiungere test `EnsureValidIdentifier_TrailingNewline_Throws` (+ caso tabella `"t"`). Verificare stessa regex nel sink PostgreSQL |
| [ ] | A53 | G10 | code | Test `MySqlBatchedSink.ExtractValue`/`FormatProperties`/`ReadProperty`/`Simplify` (rendered, level, timestamp UTC vs locale, property mancante, scalar, structure/dictionary): rendere `ExtractValue` `internal`, `LogEvent` costruiti a mano, senza DB |
| [ ] | A54 | G10 | code | Bug race `ContextualLogBuffer.FlushAndClear` (`_count = 0` non atomico, slot azzerati mentre `Push` scrive, letture non volatile; flush concorrenti duplicano): perde/duplica/tronca entry. Richiede lock breve o sequence number per slot. Test stress Push+Flush insieme al fix. Valutare anche validazione `capacity <= 0` nel costruttore |
| [ ] | A64 | G11 | code | `LoggerHelperOptions.MergeFluentFrom` (A63): le `MaskingRule` fluent sono accodate a quelle JSON, quindi regole identiche si duplicano. Deduplicare (confronto per valore) + test: `EnableSelfLogging` OR, fluent masking `Enabled=false`, regole duplicate |
| [ ] | A65 | G11 | user | **Azione utente (A61/A49):** ruotare password SMTP, BotToken Telegram, ApiKey Seq, password MySQL (e Postgres/MSSQL se non default locali); confermare che la password postgres in `appsettings.LoggerHelper.json` sia un default di sviluppo; valutare la pulizia della history (`git filter-repo`) + force push. I segreti restano nella history finché non ruotati/purgati. Non scrivere i valori in nessun file |
| [ ] | A31 | G11 | docs | Allineare i README dei sink Console, MSSqlServer, File, Elasticsearch, Seq (chiari, completi, esempi = API reale) |
| [ ] | A32 | G11 | docs | Sito: `playground.html:155` carica `assets/app.js` inesistente (c'è `js/main.js`); verificare menu in `index.html` |
| [ ] | A33 | G11 | docs | Sito: applicare "Show, Don't Tell" ai sink vecchi (esempi concreti al posto delle descrizioni) |
| [ ] | A34 | G11 | docs | README HangfireConsole: dichiarare TFM supportati (net8.0/net9.0/net10.0). Vedi `outcomes/audits/hangfireconsole-docs-review.md` |
| [ ] | A35 | G12 | code | `.mcp.json` (radice, oggi solo `perplexity-docs`): aggiungere `demo-logger` e `myapp-logger` (MCP di Demo/TestApp) con URL + trasporto corretti (oggi ECONNREFUSED); poi togliere i permessi `mcp__demo-logger__*` / `mcp__myapp-logger__*` da `.claude/settings.local.json` se ridondanti |
| [ ] | A39 | G0b | code | Parità repo pubblico `alexbypa/CSharp.Essentials`: portare fix A38 (`ReplaceLineEndings(" ")` nei due `SanitizeLogValue` + test) per mantenere identiche le due copie |
| [ ] | A40 | G0b | analysis | `Directory.Packages.props` non applicato (nessun `ManagePackageVersionsCentrally`, versioni inline nei csproj e divergenti, es. M.E.Configuration 9.0.5 vs 10.0.9): attivare CPM o rimuovere il file e correggere CLAUDE.md |
| [ ] | A41 | G0b | code | Core csproj: allineare major delle dipendenze M.E.* (Configuration/Binder 10.0.9 vs Json/Logging 9.0.1) |
| [ ] | A48 | G6b | code | `HttpHelperLog` (`src/CSharpEssentials.HttpHelper/HttpHelperLog.cs:10`) scrive sul `Serilog.Log` statico, ma `AddLoggerHelper` non assegna mai `Log.Logger`: i log interni di HttpHelper non raggiungono la pipeline LoggerHelper, contrariamente al README HttpHelper (~riga 82). Collegarlo alla pipeline oppure correggere il README (da C1) |
| [ ] | A50 | G6b | docs | Bassa priorità. Demo: il cursore round-robin del mock è condiviso, quindi la sequenza 502/503/200 vale solo per richieste seriali: annotarlo nel README della Demo oppure rendere il match per-request (da C1) |
| [ ] | A51 | G4c | code | Bassa priorità. `IHttpMockScenario`: aggiungere un default interface member che esponga le factory request-aware (`Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>`), così anche le implementazioni custom ricevono request e token nella factory (oggi solo `HttpMockScenario`; nota di design nel commento ponytail di `HttpMockEngine`). Additivo, nessun breaking change |

## Roadmap (promemoria — NON eseguire)

- [/] **MCP:** nuovi tool `set_log_level`, `search_logs`, `toggle_sink`. *(fatti: SSE transport, prompt `diagnose-logging`)*
- [/] **Dashboard:** metriche performance (events/sec, latency), sink actions (enable/disable), export JSON/CSV. *(fatti: live stream SSE, `RequireAuthorization`)*
- [/] **Performance:** test AOT/Trimming (`IsTrimmable=true`), Source Generator per auto-registration sink *(prototipo rotto rimosso in A12)*. *(fatto: benchmark vs Serilog/NLog)*
