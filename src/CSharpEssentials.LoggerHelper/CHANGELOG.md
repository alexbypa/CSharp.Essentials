# Changelog

All notable changes to **CSharpEssentials** packages are documented here.  
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).  
Versioning follows [Semantic Versioning](https://semver.org/).

---

## [5.0.4] — 2026-06-05

### Performance

- **`SinkRouting.Matches()` — hot path optimization**  
  This predicate runs for every log event, once per configured sink. The previous
  implementation called `level.ToString()` (heap allocation) and
  `List<string>.Contains()` (O(n) linear scan) on each call.  
  Now: `Levels` is converted to a `HashSet<LogEventLevel>` once at first use.
  Subsequent calls do a direct enum hash lookup — **zero allocations, O(1)**.

---

## [5.0.3] — 2026-06-03

### Added

- Sink.Email: `SmtpClient` as `readonly` field + `IDisposable` + lock for
  thread-safety on concurrent `Emit()` calls.
- Sink.Telegram: `HttpClient.Timeout = 10s` + `Task.Run` to avoid
  sync-over-async deadlock.
- Tags and metadata improvements across all packages.

---

## [5.0.2] — 2026-05-30

### Added

- SEO optimization: `Description` and `Tags` updated for all 11 NuGet packages.
- Individual `README.md` per package with quick-start and configuration examples.

---

## [5.0.1] — 2026-05-28

### Added

- Sink.File: dynamic per-property file routing (`FileNameProperty`) for
  multi-tenant log separation.
- `ILogErrorStore`: injectable diagnostic store for sink failure inspection
  at runtime without crashing the application.