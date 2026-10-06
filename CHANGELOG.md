# Changelog

All notable changes to **CSharpEssentials** packages are documented here.  
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).  
Versioning follows [Semantic Versioning](https://semver.org/).

---

## [Unreleased]

### Added

- **Demo:** interactive Playground (`/playground.html`, now the Demo root) to try Console, File, Seq, Elasticsearch, MSSqlServer, PostgreSQL and Telegram (bot token from user-secrets) with the fluent API: per-sink minimum level, structured properties, sensitive data masking (presets, properties, regex rules), optional exception. Each request builds a new logger, writes one event, flushes it and shows which sinks received it and the event after masking. **HttpHelper** section: real calls to httpbin.org (success, flaky 5xx with retries, down, timeout → 408, Bearer auth with masked token, POST JSON, rate limit → 429), every attempt logged to the chosen sinks; base URL configurable with `Playground:HttpBaseUrl`. `docker/docker-compose.yml` starts the external sinks (SQL Server, PostgreSQL, Seq, Elasticsearch, optional Kibana). Scalar API reference on `/scalar` next to Swagger. Demo only; the two library fixes it uncovered are listed under Security and Fixed.

- **Site:** new comparison page `compare.html` (LoggerHelper vs Serilog vs NLog, incl. benchmark overhead), linked from the home footer and `llms.txt`. Marketing drafts (dev.to x2, YouTube script, Reddit) in `outcomes/content/`, not published.
- **HttpHelper** now also targets `net10.0` (`net8.0;net9.0;net10.0`), aligned with the core and the sinks.
  `HttpHelper.Tests` runs on net9.0 and net10.0. No public API change. The existing nullable warnings now repeat for the new target.

- **Tests:** `MySqlColumnMap` and `MySqlBatchedSink` (identifier injection guard, column mapping, generated `INSERT`/`CREATE TABLE`), no DB needed. `Sink.MySql` exposes internals to `CSharpEssentials.LoggerHelper.Tests`. No public API change.

- **Tests:** `RequestResponseLoggingMiddleware` (body capture, truncation limits, status ≥400, error path).

- **Tests:** `AddLoggerHelper(IConfiguration)` (no routes, legacy fallback, fluent merge, contextual buffer registration, JSON file in cwd, Development file) with isolated cwd/env.

- **Tests:** `Configure` of the Email, MSSqlServer, PostgreSQL, MySQL, Telegram, HangfireConsole, Seq and Elasticsearch sink plugins (missing config -> `InvalidOperationException`, minimal config -> logger built, no network). `Sink.HangfireConsole` exposes internals to `CSharpEssentials.LoggerHelper.Tests`.

- **Tests:** legacy `loggerExtension<T>` (enrichment, null request/args, `SpanName`, Dashboard and async variants).

- **Tests:** `ContextualLogBuffer` and `ContextualLogSink` (ordering, wrap-around, flush, memory release, Error/Fatal replay). No production change.

- **Demo**: HttpHelper integration scenario (`GET /api/httphelper/retry`, mocked flaky upstream, retries logged through LoggerHelper and visible in the dashboard).

- **Elasticsearch sink:** new option `AutoRegisterTemplate` (default `true`, JSON key `AutoRegisterTemplate` / legacy `autoRegisterTemplate`). Set `false` to skip the synchronous index-template HTTP call at startup (about 2 s when the node is down). Default behavior unchanged.

- **HttpHelper** mocks: new public constructor `HttpMockScenario(match, List<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>)`, so a response factory receives the request and the cancellation token. Existing constructors are unchanged (see the source-level caveat under Fixed).

### Changed

- **Demo:** `/health` now reports `configured` for each loaded sink, so failed sinks no longer look loaded. Internal cleanups in `SinkRoutingEngine` (single `LoadedSinkInfo` construction).
- **Sinks (behavioral, no signature change):** `ISinkPlugin.Configure` now throws `InvalidOperationException` when the sink section is missing or a required option is empty (Email `Host`/`From`/`To`; MSSqlServer, Postgresql, MySql `ConnectionString`; Telegram `BotToken`/`ChatId`; Seq `ServerUrl`; Elasticsearch `NodeUris`; HangfireConsole requires `AddHangfireConsoleSink()` before `AddLoggerHelper()`). Previously most sinks returned silently (or failed at runtime). `SinkRoutingEngine` catches the exception, keeps the other sinks working and records `LoadedSinkInfo.Configured = false`, so the Dashboard and MCP `loggerhelper_get_sinks` show the sink as FAILED instead of ACTIVE. Custom sink authors should follow the same contract.
- **HttpHelper** docs: package README rewritten (Quick Start, features table, JSON/Fluent examples, comparison vs Refit/Flurl/HttpClient) and new site page `httphelper.html`. The stale "4.0.5" `AddHttpClients(configuration, handler)` snippet is removed: no such overload exists, use `UseCompression`/`httpProxy`/`Certificate` in `HttpClientOptions`.
- **HttpHelper** mock engine: `HttpMockEngine.Build()` now uses an internal `HttpMessageHandler` instead of Moq. Public API unchanged. When several scenarios match, the last one wins; responses cycle round-robin (thread-safe). A request with no matching scenario (or a scenario with no `ResponseFactory`) throws `InvalidOperationException` (previously Moq's `MockException`), and `HttpMockDelegatingHandler` no longer falls back to the real network: any exception thrown by a matched scenario (e.g. a simulated `HttpRequestException`) now reaches the caller, where the helper turns it into a 502 as for a real transport failure.

### Removed

- **HttpHelper** no longer depends on `Moq`; it is no longer pulled in transitively by the NuGet package.

### Security

- **Sensitive data masking:** with `EnableRenderedMessage` on, a value masked only through `SensitiveProperties` (e.g. `Password`, which no regex preset matches) leaked in clear text in the `RenderedMessage` property, because the message was rendered before masking. When any property is masked, `RenderedMessage` is now re-rendered from the masked properties and then scrubbed by the presets/rules. It reached Seq, Elasticsearch and the properties/JSON columns of DB sinks. Found with the Demo Playground. No public API change.

- Log-forging sanitization in `RequestResponseLoggingMiddleware` and legacy `TraceSync` now also
  neutralizes the Unicode line terminators NEL (U+0085), LS (U+2028) and PS (U+2029), plus form feed,
  replacing them with a space. A lone CR is now replaced with a space instead of being removed.
  No public API change.

- **Demo:** `appsettings.LoggerHelper.debug.json` was tracked with real-looking credentials (SMTP, Telegram, Seq, MySQL) and is now untracked; copy `appsettings.LoggerHelper.debug.example.json` and fill in your own values. The old values **remain in git history, so those credentials must be rotated**. `SeqVerifier.js` no longer prints the Seq `apiKey`.

### Fixed

- **Legacy `loggerExtension<T>`:** each `AddLoggerHelper` call makes its logger the legacy one, so a short-lived logger built and disposed at runtime (e.g. the Demo Playground) left the legacy API writing to a disposed logger. Disposing a logger now hands the legacy API back to the logger that was current before it (only if it is still the current one). No public API change.

- **`AddLoggerHelper(config, fluent)` (behavior change):** fluent `General` flags and `SensitiveDataMasking` were silently discarded when JSON was also present. They are now merged over the JSON: `EnableSelfLogging`, `EnableRequestResponseLogging`, `EnableRenderedMessage` are OR-ed; `EnableOpenTelemetry` is AND-ed (fluent `DisableOpenTelemetry` can only turn it off); masking is additive (union of presets and properties, rules appended, fluent `MaskText` used when non-default).

- **RequestResponseLoggingMiddleware:** when downstream throws before the response has started, the partial body is no longer copied to the real `Response.Body`, so an upstream `UseExceptionHandler` can still set the 500 (supersedes the note in the earlier entry below). Truncation at `MaxBodySize` no longer splits surrogate pairs.

- **AddLoggerHelper(config, fluent):** the "no routes" validation now runs after the fluent merge, so routes defined only through the fluent API work over a JSON with no routes. Documented: an `appsettings.LoggerHelper*.json` in the working directory fully replaces the passed `IConfiguration`.
- **AddLoggerHelper(config, fluent):** sink options set through the fluent API (`ConfigureSeq`, `ConfigureEmail`, …) are now merged into the JSON options and override the JSON entry for the same sink. Before, they were dropped and the sink failed to configure.

- **loggerExtension<T>** (legacy v2–v4 API): `TraceDashBoardSync` / `TraceDashBoardAsync` no longer throw when `args` is `null` (the `async void` variant could crash the process); now consistent with `TraceSync`.

- **AddLoggerHelper(IConfiguration):** the `PhysicalFileProvider` used to read `appsettings.LoggerHelper*.json` from the current directory is now disposed and no longer watches the file (`reloadOnChange: false`); it leaked one `FileSystemWatcher` per call. Options are bound once at startup, so the file is not reloaded at runtime (it never reconfigured the already-built sinks).

- **RequestResponseLoggingMiddleware:** restores the original `Response.Body` after the request (it was left on a disposed buffer, breaking upstream exception handlers); when downstream throws after a partial write, the partial response was copied to the client instead of an empty body (reverted when the response has not started, see above); request/response bodies delivered in small chunks are now logged in full and the body limit check no longer does synchronous I/O (Kestrel rejects it); pooled body buffers are cleared on return.

- **HttpHelper** mocks ignored timeout and caller cancellation: a slow mock with `addTimeout` returned a late 200 instead of 408, and cancelling the caller's token did not stop it. The engine now stops waiting (`WaitAsync(ct)`) for every mock factory, legacy ones included: slow mock + timeout gives 408, caller cancellation throws `OperationCanceledException`. Behavior note: legacy factories are now interruptible, and a late response abandoned this way is not disposed.
- **Source-level caveat:** `new HttpMockScenario(match, [])` and `new HttpMockScenario(match, null)` are now ambiguous (CS0121) between the two list overloads; pass a typed list (e.g. `new List<Func<Task<HttpResponseMessage>>>()`). Binary compatibility is unaffected.

- **HttpHelper**
  - Retry no longer blocks a thread to clone the request body (`.Result` → `await`).
  - `SendAsync` no longer swallows internal errors and then throws `NullReferenceException`:
    an exceeded rate limit now returns a `429` response, an empty URL throws `InvalidOperationException`;
    the rate-limit lease is disposed.
  - `addTimeout`: only the helper's own timeout is enforced (`HttpClient.Timeout` is set to infinite),
    so a timeout deterministically returns `408` instead of racing with `HttpClient` (`500`).
  - Callbacks (`AddRequestAction`) are now per client: a callback registered on one named client no longer
    fires for every client. `HttpRequestEvents` is thread-safe.
  - `httpsClientHelperFactory`: one helper per name even under concurrency (`ConcurrentDictionary` + `Lazy`);
    `AddActionOnRequest(callback)` no longer throws when called before `CreateOrGet`
    (it registers a global callback and returns the last created helper, or `null`).
  - `AddHttpClients` registers `IHttpClientFactory` even with no configured client, and registers
    named clients only (the typed `IhttpsClientHelper` registration was not resolvable).
  - Client certificate: missing/empty path or missing file is logged and skipped instead of continuing.
  - Synthetic `502`/`500` error bodies are now valid JSON (messages are escaped).
  - `SendAsync`: cancelling the caller's `CancellationToken` now throws `OperationCanceledException`
    (it was returned as a synthetic `500`); a timeout from the default `HttpClient.Timeout` (no `addTimeout`) now returns `408` instead of `500`.
    **Behavior change** for code that relied on a `500` response after cancelling (suggested bump: minor).
  - `addTimeout` after the first `SendAsync` no longer throws `InvalidOperationException`: it no longer touches
    `HttpClient.Timeout`. The helper never changes an `HttpClient` passed to its constructor (it may be shared):
    the client's own timeout still applies and the shorter of the two wins, both returned as `408`.
    Helpers created by `IhttpsClientHelperFactory` own their client, so there `addTimeout` can also be longer
    than the configured client timeout (which stays the helper's default).
  - A request callback that cancels itself (not the request) is logged and ignored instead of turning a
    successful response into a `408`.
    Calling `addTimeout` again now replaces the previous value (before, every call after the first was silently ignored).
  - A callback registered with `AddRequestAction` that throws no longer fails the HTTP response nor stops the other
    callbacks: the error is logged and ignored (cancellation still propagates).
  - The request (and every retry clone) is disposed when `SendAsync` completes. Do not read `response.RequestMessage.Content`
    after the call: headers and URI stay readable, the content is disposed.
  - Timestamps use UTC; `RequestHttpExtension.IdTransaction` is now a unique GUID (`N` format) instead of a local date string.

### Changed

- **HttpHelper (behavior)**
  - `AddHttpClients` no longer prints to the console and no longer reads `appsettings.httphelper.json`
    from the application folder (nor adds environment variables on top of it): use the
    `HttpClientOptions` section of the host configuration.
  - New `IhttpsClientHelperFactory.AddActionOnRequest(string name, callback)` (default interface method)
    and `HttpRequestEventsRegistry`; `HttpClientHandlerLogging` and `httpsClientHelperFactory` gain an overloaded constructor.
  - `HttpHelper.Tests` grew from 22 to 45 tests (rate limiter, cancellation, timeout `408`, error mapping,
    form data, default headers, factory concurrency, per-client callbacks, certificate guards, `ProxyConfigurator` invalid address).

### Removed

- Orphan `CSharpEssentials.LoggerHelper.SourceGenerator` project (never published) and the internal
  dead `CompileTimePluginDiscovery` / `CompositePluginDiscovery`. Plugin discovery is now
  `FileSystemPluginDiscovery` only. No public API change, no runtime behavior change.
- Dead `net6.0` package group from the `CSharpEssentials.LoggerHelper` project file (the package
  targets net8.0/net9.0/net10.0 only). Package dependencies are unchanged for every target.
- Broken root-level `CSharpEssentials.LoggerHelper.slnx` (its project paths did not resolve).
  The only solution is now `src/CSharpEssentials.LoggerHelper.slnx`.

### Changed

- **CI / tests (internal, no API change)**
  CI now builds and tests `CSharpEssentials.HttpHelper`: `HttpHelper.Tests` was added to
  `src/CSharpEssentials.LoggerHelper.slnx` and `build-test.yml` runs it.
  New smoke test `AllPublishedSinks_Register_AndCanHandle` verifies that all 10 published
  sinks register in `SinkPluginRegistry` and handle their own name.
  `HttpHelper.Tests` grew from one trivial test to 22 network-free tests covering
  `HttpClientOptions` defaults, `ProxyConfigurator`, `AddHttpClients` registration,
  `HttpsClientHelperFactory` and `HttpsClientHelper` (headers, JSON body, retry, error mapping).
  Sink auto-registration is now tested without any explicit `PluginInitializer.Init()` call:
  `AddLoggerHelper` rediscovers all 10 sinks from an empty `SinkPluginRegistry`, and each sink's
  `[ModuleInitializer]` registers its plugin when the module loads in a fresh `AssemblyLoadContext`.
  Seven new tests cover the request/response middleware and legacy `TraceSync` log sanitization.
- **Build (internal, no API change)**
  The intentional `CA2255` (`[ModuleInitializer]` in a library) is silenced only in the
  `CSharpEssentials.LoggerHelper.Sink.*` projects, via a conditional `NoWarn` in `src/Directory.Build.props`
  (build warnings 90 → 60). New sinks inherit it automatically.

---

## [5.2.6] — 2026-08-31

### Security

- **Log forging (CodeQL `cs/log-forging`)**
  `RequestResponseLoggingMiddleware` now sanitizes user-controlled values before logging them:
  HTTP method, path, unescaped query string, request/response bodies (and method/path in the error log).
  The legacy `loggerExtension<T>.TraceSync` sanitizes `IdTransaction` and `Action`.
  Sanitization removes CR and replaces LF with a space, so forged log lines cannot be injected.
  Behavior change: multi-line request/response bodies are now logged on a single line.
  No public API change.

### Documentation

- README: MySQL / MariaDB sink listed in the package and sink tables; expanded
  `CSharpEssentials.LoggerHelper.Sink.MySql` README (shipped as 5.2.4 and 5.2.4.1, docs-only releases).
  Note: 5.2.5 was not published.

---

## [5.2.3] — 2026-08-25

### Added

- **MySQL / MariaDB sink — structured logs in real columns**
  New package `CSharpEssentials.LoggerHelper.Sink.MySql` brings the tenth sink to the
  ecosystem. Routes accept `MySql`, `MySQL` or `MariaDB` (case-insensitive).

  ```json
  {
    "LoggerHelper": {
      "Routes": [ { "Sink": "MySql", "Levels": ["Warning", "Error", "Fatal"] } ],
      "Sinks": {
        "MySql": {
          "ConnectionString": "Server=localhost;Database=logs;Uid=app;Pwd=secret;",
          "TableName": "app_logs",
          "AutoCreateTable": true,
          "StoreTimestampInUtc": true
        }
      }
    }
  }
  ```

  **Built in-house rather than wrapping a third-party sink.** The two candidates on NuGet
  each forced a trade-off worth avoiding:
  - `Serilog.Sinks.MySQL` ships a **fixed schema**, so `ApplicationName`, `IdTransaction`,
    `Action` and `MachineName` would have been buried inside a JSON blob instead of
    being queryable columns — no parity with the PostgreSQL sink.
  - `Serilog.Sinks.MariaDB` supports custom columns but pins **Serilog 2.10** and
    `Serilog.Sinks.PeriodicBatching 2.3.0` against the core's Serilog 4.2, and targets
    `netstandard` only.

  The sink instead sits directly on `MySqlConnector 2.5.0` +
  `Serilog.Sinks.PeriodicBatching 5.0.0` (Serilog 4.x), keeping the dependency graph
  aligned with the rest of the ecosystem.

  **Feature parity with the PostgreSQL sink:**
  - Same ten default columns — `ApplicationName`, `message`, `message_template`, `level`,
    `raise_date`, `exception`, `properties`, `MachineName`, `Action`, `IdTransaction`
  - Same eight `Writer` kinds — `Rendered`, `Template`, `Level`, `Timestamp`, `Exception`,
    `Serialized`, `Properties`, `Single` — so an existing `Columns` block ports over by
    adjusting only the `Type` values
  - `AutoCreateTable` issues `CREATE TABLE IF NOT EXISTS` with
    `utf8mb4 / utf8mb4_unicode_ci`, optional `Id BIGINT AUTO_INCREMENT PRIMARY KEY`

  **MySQL-specific behaviour:**
  - `StoreTimestampInUtc` (default `false`) — MySQL has no `timestamptz` equivalent, so
    the sink emits `DATETIME(6)` and lets you pick the clock. `DATETIME(6)` is used over
    `TIMESTAMP` deliberately: the latter is capped at 2038-01-19
  - `Type: "Json"` maps to native `JSON` on MySQL 5.7.8+, and to the `LONGTEXT` alias on
    MariaDB
  - Batched async writes, one transaction per batch, `BatchPostingLimit` clamped to
    `1..1000`, bounded 10,000-event queue
  - Write failures are reported through Serilog `SelfLog` and never thrown into the host

  **Security:** table and column names arriving from configuration are validated against
  `^[A-Za-z0-9_]{1,64}$` and backtick-quoted; every value is passed as a command
  parameter, never concatenated into SQL.

  Targets `net8.0`, `net9.0`, `net10.0`. Registered in both `.slnx` files and in
  `LegacyConfigurationAdapter`, so the legacy `SerilogOption:MySql` section binds too.
  CI needed no change — `publish.yml` globs `Sink.*` projects.

  > **Verified at compile time only.** The sink has not yet been exercised against a live
  > MySQL or MariaDB server; DDL generation, inserts and column mapping still need a
  > runtime pass before this is considered production-ready.

### Documentation

- **`CSharpEssentials.LoggerHelper.Sink.MySql` README**
  Follows the spoke-README structure established in 5.2.2, plus four MySQL-specific
  sections: using an existing table, timestamps and time zones, migrating from the
  PostgreSQL sink (type mapping table), and behaviour/reliability.

  Includes a warning that MySQL runs `STRICT_TRANS_TABLES` by default since 5.7 and
  **rejects** over-long values with `ERROR 1406` instead of truncating — the sink does not
  truncate either, so an undersized `VARCHAR` costs the whole batch.

---

## [5.2.2] — 2026-07-02

### Documentation

- **Spoke READMEs — full rewrite for Console, File, Elasticsearch, MSSqlServer, Seq sinks**
  Every spoke README now includes: targets header (`net8.0 · net9.0 · net10.0`), the
  required `app.UseLoggerHelper()` call (previously missing from all five), a "What You'll
  See" section with actual output format, platform path examples (Windows + Linux for File),
  an OpenSearch compatibility note (Elasticsearch), and a Troubleshooting table.

  Key accuracy fixes:
  - **Console** — documented real output format from `ColoredConsoleSink.Emit()`:
    `[HH:mm:ss Level] message`
  - **Elasticsearch** — removed non-existent `Username`/`Password` from config table;
    clarified `autoRegisterTemplate` is hardcoded (not user-configurable)
  - **MSSqlServer** — documented `Period` format (`d.hh:mm:ss`), listed valid
    `AddStandardColumns` enum values, added `AdditionalColumns` end-to-end example
  - **File** — documented `FileNameProperty` multi-tenant subdirectory routing with
    path examples and `@t`/`@mt`/`@l`/`@x` field reference table
  - **Seq** — added Docker quickstart, Seq query language examples, clarified `ApiKey`
    is optional for local single-user Seq

- **Hub README (root) — sync with spoke guides**
  Fixed Italian text in "Run the Demo" section (→ English), removed duplicate
  HangfireConsole row from Packages table, added `[guide →]` links to the five updated
  spoke READMEs, fixed Elasticsearch JSON example (removed non-existent `Username`/`Password`
  fields), updated MCP section to document `app.MapLoggerHelperMcpSse()` and the
  `diagnose-logging` predefined prompt, replaced verbose `<details>` Sink Overview blocks
  with a clean navigable table.

---

## [5.1.0] — 2026-06-16

### Added

- **MCP Server — AI assistant tooling via Model Context Protocol** *(killer feature — see [growth audit](outcomes/audits/v5.1.0-growth-audit.md))*  
  New package `CSharpEssentials.LoggerHelper.MCP` adds a zero-dependency MCP server
  (JSON-RPC 2.0, Streamable HTTP transport) to any ASP.NET Core application that already
  uses LoggerHelper. Two lines of setup expose four tools to any MCP-compatible AI client:

  ```csharp
  builder.Services.AddLoggerHelperMcp();
  // ...
  app.MapLoggerHelperMcp("/mcp");
  ```

  **Exposed tools:**
  - `loggerhelper_get_health` — overall status (OK / WARNING / CRITICAL), sink count, error count
  - `loggerhelper_get_errors` — recent sink errors from `ILogErrorStore` (accepts `count` param)
  - `loggerhelper_get_sinks` — all configured routes with ACTIVE/FAILED status and log levels
  - `loggerhelper_get_config` — application name, routing rules, masking settings

  **Why this matters:** Serilog, NLog, and every other .NET logging library requires a separate
  dashboard (Seq, Kibana, Grafana) to give AI assistants visibility into log state. LoggerHelper
  MCP ships that capability built-in — zero extra infrastructure, zero extra dependencies
  (pure `System.Text.Json` + ASP.NET Core), one NuGet package.

  Compatible with: Claude, Cursor, GitHub Copilot, any MCP HTTP client.

  Demonstrated end-to-end in `CSharpEssentials.LoggerHelper.Demo` at:
  - `POST /mcp` — full JSON-RPC 2.0 server
  - `GET /api/mcp-demo/tools` — discovery endpoint with curl examples
  - `POST /api/mcp-demo/call/{toolName}` — REST shortcut for manual testing

### Improved (NuGet SEO — FASE 1)

- **All 11 packages** — `<PackageTags>` expanded with `.NET` version targets (`dotnet8`, `dotnet9`, `dotnet10`),
  ecosystem terms (`zero-boilerplate`, `ilogger`, `aspnetcore`, `minimal-api`), and
  technology-specific search terms per sink (e.g., `jsonb`, `ilm`, `live-tail`, `push-notifications`).
- **Console sink** — `<Description>` updated to remove stale "5.0.1 File sink" mention.
- **File sink** — duplicate `<PackageTags>` entry removed; `<Description>` added with v5.0.7 perf details.
- **Email sink** — `<Description>` clarified: highlights throttle, template caching, zero-dependency.
- **Telegram sink** — `<Description>` highlights fire-and-forget, throttle, zero-dependency.
- **HangfireConsole sink** — `<Description>` rewritten to remove confusing "bug fix" framing.
- **Core package** — `<Description>` updated to headline the MCP server as the v5.1.0 feature.

---

## [5.0.8] — 2026-06-13

### Added

- **Sensitive Data Masking — declarative, JSON-driven PII/secret redaction** *(killer feature — see [growth audit](outcomes/audits/v5.0.8-growth-audit.md))*
  New `SensitiveDataMaskingEnricher`, opt-in via `LoggerHelper:SensitiveDataMasking` (JSON) or
  `.EnableSensitiveDataMasking(...)` (fluent API). One configuration block protects **every**
  configured sink — Console, File, SQL Server, PostgreSQL, Elasticsearch, Seq, Telegram, Email —
  with zero changes at logging call sites.
  - Built-in presets: `Email`, `CreditCard`, `JwtToken`, `BearerToken`, `ConnectionStringSecret`.
  - `SensitiveProperties`: structured property names (e.g. `Password`, `ApiKey`) replaced outright
    regardless of content.
  - Custom `Rules`: arbitrary regex patterns, with an optional named `secret` capture group to mask
    only part of a match (e.g. keep `Bearer ` / `Password=` visible, redact only the value).
  - Disabled by default — zero overhead unless explicitly enabled.
  - Demonstrated end-to-end in `CSharpEssentials.LoggerHelper.Demo` via `/api/masking/*` endpoints.

---

## [5.0.7] — 2026-06-11

### Performance

- **`Sink.File` — `DynamicPropertyFileSink.ResolveSink()` hot path** *(measured — see [benchmarks](docs/benchmarks.md))*
  When `FileNameProperty` is configured for multi-tenant log routing, this method
  ran on **every single log event**. The previous implementation called
  `ConcurrentDictionary.GetOrAdd(key, factoryLambda)` even on a cache hit — the
  `Func<string, SinkEntry>` closure captures `this` and the C# compiler cannot
  cache it, so a new delegate was allocated per event. It then called
  `EvictIfNeeded()` unconditionally, which reads `ConcurrentDictionary.Count` —
  an operation that acquires every internal table lock.
  Now: a `TryGetValue` fast path returns the cached per-tenant sink with zero
  delegate allocations, and eviction is gated by a cheap `Interlocked` counter so
  `Count` is only touched when a brand-new tenant sink is created.
	
### Fixed

- **`Sink.File` — leaked Serilog file logger under concurrent first-write race**
  If two threads logged for the same brand-new `FileNameProperty` value (e.g. a
  new tenant's first request) at the same time, `GetOrAdd`'s factory could run
  twice; the "losing" Serilog file logger — and its open file handle — was never
  disposed. The new code explicitly disposes the redundant logger when it loses
  the race.
	
---

## [5.0.6] — 2026-06-08
-  RequestResponseLoggingMiddleware : ArrayPool<char>.Shared.Rent(MaxBodySize) returns a buffer from the shared pool—zero heap allocation. The buffer is returned in a finally block. Also replaced the legacy overload ReadAsync(char[], int, int) with the modern ReadAsync(Memory<char>).

---

## [5.0.5] — 2026-06-06
-  SinkThrottlingManager` CAS loop**<br>• *Correctness*<br>• Eliminates duplicate sends under concurrency | Prevents duplicate actions during concurrent burst events 
- `SinkPluginRegistry` ConcurrentDictionary**<br>• *Correctness + Performance*<br>• Idempotent registration; $O(1)$ duplicate check | Eliminates linear scans and race conditions during startup registration 
- `TelegramSinkPlugin` fire-and-forget**<br>• *Critical Performance*<br>• `Emit()` no longer blocks the Serilog pipeline | Eliminates multi-second blocking I/O on the logging thread 
- `EmailSinkPlugin` constructor cache**<br>• *Performance*<br>• Removes disk I/O and `SmtpClient` allocs from hot path | Prevents file system overhead and connection churn per log event 

## [5.0.4] — 2026-06-05

### Fixed

- **`SinkThrottlingManager.CanSend()` — race condition (TOCTOU)**  
  Two concurrent threads could both pass the throttle window check and both send
  (e.g., two simultaneous emails or Telegram messages). Fixed with a
  `ConcurrentDictionary.TryUpdate()` compare-and-swap loop — the slot is now
  claimed atomically.

- **`EmailSink` — `File.ReadAllText` on every `Emit()`**  
  When a custom `TemplatePath` was configured, the HTML template was read from
  disk on every single email send. The template is now loaded once at sink
  construction time and cached in memory.

### Performance

- **`SinkRouting.Matches()` — hot path optimization** *(measured — see [benchmarks](docs/benchmarks.md))*  
  This predicate runs for every log event, once per configured sink. The previous
  implementation called `level.ToString()` (heap allocation) and
  `List<string>.Contains()` (O(n) linear scan) on each call.  
  Now: `Levels` is converted to a `HashSet<LogEventLevel>` once at first use.
  Subsequent calls do a direct enum hash lookup — **zero allocations, O(1)**.  
  Measured result: **~5× faster** on average (2.6 ns vs 12.8 ns), **up to 8×** on
  miss with 5 configured levels; allocated memory drops from 24 B to **0 B** per call.
  At 1 000 log/sec with 4 sinks: 96 KB/sec of string garbage eliminated.

- **`SinkPluginRegistry` — O(1) duplicate detection**  
  Registration (called via `[ModuleInitializer]` at startup) previously used
  LINQ `.Any()` over a `ConcurrentBag`, which is O(n) and not atomically safe.
  Replaced with `ConcurrentDictionary<Type, ISinkPlugin>.TryAdd()`.

- **`TelegramSink.Emit()` — non-blocking network I/O**  
  `Task.Run(() => SendAsync()).GetAwaiter().GetResult()` was blocking Serilog's
  background thread for up to 10 seconds (the HTTP timeout) on every message.
  Changed to true fire-and-forget — the Serilog queue thread is released
  immediately; errors are forwarded to `SelfLog`.

- **`FileSink.SanitizeFileName()` — compiled Regex**  
  The static `Regex.Replace(value, pattern)` overload uses an internal LRU cache
  capped at ~15 entries and risks recompilation under load. Replaced with a
  `static readonly` field using `RegexOptions.Compiled`.

### Added

- **`src/samples/LoggerHelper.QuickStart`** — self-contained Minimal API (.NET 9)
  demonstrating all log levels, `BeginScope`, and the `/health/logging` endpoint.
  Clone the repo and run `dotnet run` to see logs on console and in `Logs/`
  within seconds.

- **`tools/track-downloads.py`** — Python script that polls the NuGet Search API
  for all 11 packages and appends `date, package, total_downloads, version` rows
  to `tools/downloads.csv`. Schedule daily via cron to track growth over time.

---

## [5.0.3] — 2026-04-18

### Added

- Sink.Email: `SmtpClient` as `readonly` field + `IDisposable` + lock for
  thread-safety on concurrent `Emit()` calls.
- Sink.Telegram: `HttpClient.Timeout = 10s` + `Task.Run` to avoid
  sync-over-async deadlock.
- Tags and metadata improvements across all packages.

---

## [5.0.2] — 2025-12-01

### Added

- SEO optimization: `Description` and `Tags` updated for all 11 NuGet packages.
- Individual `README.md` per package with quick-start and configuration examples.

---

## [5.0.1] — 2025-10-15

### Added

- Sink.File: dynamic per-property file routing (`FileNameProperty`) for
  multi-tenant log separation.
- `ILogErrorStore`: injectable diagnostic store for sink failure inspection
  at runtime without crashing the application.

---

## [5.0.0] — 2025-09-01

### Breaking changes from v4

- Complete architectural rewrite: plugin system via `ISinkPlugin` +
  `[ModuleInitializer]` auto-registration.
- New fluent builder API (`AddLoggerHelper(b => b.AddRoute(...).Configure...())`).
- New JSON schema (`LoggerHelper:Routes` + `LoggerHelper:Sinks`).
- Legacy `Serilog:SerilogConfiguration` (v2–v4) still supported via
  `LegacyConfigurationAdapter` — no immediate migration required.
- Native `ILogger<T>` bridge: zero code changes for existing apps.
- `ILogErrorStore`, `ILoadedSinkStore`, `ISinkPluginRegistry` registered in DI
  for observability and testing.
