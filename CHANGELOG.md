# Changelog

All notable changes to **CSharpEssentials** packages are documented here.  
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).  
Versioning follows [Semantic Versioning](https://semver.org/).

---

## [5.2.0] — 2026-06-27

### Added

- **Contextual Error Logging — Zero-Allocation Ring Buffer** *(killer feature)*
  New `ContextualLogBuffer` retains the last N log entries (Debug/Info/Warning) in a pre-allocated,
  lock-free ring buffer. When an Error/Fatal occurs, the buffer automatically flushes all context
  entries to your configured sinks with `IsContextualHistory = true` — giving you the "what happened
  before the crash" without keeping verbose logging on permanently.
  - Thread-safe via `Interlocked` — zero locks, zero allocations after startup
  - Configurable capacity (default: 100 entries)
  - Non-destructive `Snapshot()` for MCP search and Dashboard display
  - Enable via JSON: `"General": { "EnableContextualLogging": true, "ContextualBufferCapacity": 200 }`

- **3 New MCP Tools — AI-controlled logging** *(7 tools total)*
  - `loggerhelper_set_log_level` — change log level routing for any sink at runtime via AI
  - `loggerhelper_search_logs` — query the contextual ring buffer with text/level filters
  - `loggerhelper_toggle_sink` — enable/disable a sink without application restart
  The only .NET logging library where AI can **control** logging, not just read it.

- **Embedded Dashboard** *(new package: `CSharpEssentials.LoggerHelper.Dashboard`)*
  Zero-dependency HTML UI served at `/loggerhelper` — no Seq, Kibana, or external tools needed.
  - Real-time status cards (health, sink count, errors, buffer)
  - Sink table with ACTIVE/FAILED badges and toggle controls
  - Live log stream via Server-Sent Events with level/text filters
  - Click-to-expand error history with stack traces
  - Routing configuration display
  - Dark theme, mobile-responsive, auto-refresh
  - Optional `RequireAuthorization` for production use

### Improved

- **SinkRouting** — added `InvalidateLevelCache()`, `StashAndClearLevels()`, `RestoreStashedLevels()`
  for runtime log level mutation by MCP tools and Dashboard controls
- **MCP GetConfig** — now includes contextual logging status in output
- **Demo app** — all v5.2.0 features are demonstrated and testable

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
