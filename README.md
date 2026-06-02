<p align="center">
  <img src="CSharpEssentials.LoggerHelper/img/CSharpEssentials.png" alt="CSharpEssentials.LoggerHelper Logo" width="140" />
</p>

<h1 align="center">CSharpEssentials.LoggerHelper</h1>

<p align="center">
  <strong>Route every Serilog sink by log level — zero boilerplate, native <code>ILogger&lt;T&gt;</code>, one JSON file.</strong>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper">
    <img src="https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg?label=NuGet&color=blue&logo=nuget" alt="NuGet Version" />
  </a>
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper">
    <img src="https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg?label=downloads&color=brightgreen" alt="NuGet Downloads" />
  </a>
  <img src="https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-512BD4?logo=dotnet" alt=".NET Versions" />
  <a href="LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-green.svg" alt="MIT License" />
  </a>
  <a href="https://github.com/alexbypa/CSharp.Essentials/commits/main">
    <img src="https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?color=blue" alt="Last Commit" />
  </a>
  <a href="https://www.loggerhelper.com">
    <img src="https://img.shields.io/badge/docs-loggerhelper.com-blue" alt="Documentation" />
  </a>
</p>

---

> **Not another logging framework.** LoggerHelper is a **Serilog orchestrator** that plugs in as a native `ILogger<T>` provider and routes each log level to exactly the sinks you want — using only a JSON file. Your application code doesn't change. Your logging configuration becomes declarative, portable, and readable by anyone on the team.

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
```

---

## Table of Contents

- [The Boilerplate Problem](#-the-boilerplate-problem)
- [Packages](#-packages)
- [Quick Start](#-quick-start--30-seconds)
- [Feature Highlights](#-feature-highlights)
- [Sink Overview](#-sink-overview)
- [Comparison](#-comparison-vs-pure-serilog--nlog)
- [Performance](#-performance)
- [Coming Soon](#-coming-soon)
- [Architecture](#-architecture)
- [Documentation & Links](#-documentation--links)

---

## The Boilerplate Problem

Every team that uses Serilog in production ends up writing the same 40-line `Program.cs` setup block. It works — but it accumulates over time into a maintenance burden: hardcoded log levels, duplicated filter expressions, conditional `#if DEBUG` blocks, and zero visibility into what is routing where.

### Before — Pure Serilog (typical production setup)

```csharp
// Program.cs — 40+ lines, fragile, hard to read at a glance

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information || e.Level == LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Warning)
        .WriteTo.File("Logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
        .WriteTo.Email(new EmailConnectionInfo {
            FromEmail = "alerts@myapp.com",
            ToEmail = "ops@myapp.com",
            MailServer = "smtp.myapp.com",
            Port = 587,
            EmailSubject = "Application Error"
        }))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
        .WriteTo.Telegram(botToken: "...", chatId: "..."))
    .CreateLogger();

builder.Host.UseSerilog();
// + request/response logging: another 15-20 lines
```

### After — LoggerHelper v5 (the same setup)

**`Program.cs`** — 2 lines, forever.
```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
app.UseLoggerHelper();
```

**`appsettings.LoggerHelper.json`** — declarative, diff-friendly, portable.
```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console",  "Levels": ["Information", "Warning"] },
      { "Sink": "File",     "Levels": ["Information", "Warning", "Error", "Fatal"] },
      { "Sink": "Email",    "Levels": ["Error", "Fatal"] },
      { "Sink": "Telegram", "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "File":     { "Path": "Logs", "RollingInterval": "Day", "RetainedFileCountLimit": 7 },
      "Email":    { "From": "alerts@myapp.com", "To": "ops@myapp.com", "Host": "smtp.myapp.com", "Port": 587 },
      "Telegram": { "BotToken": "123:ABC...", "ChatId": "-100..." }
    },
    "General": { "EnableRequestResponseLogging": true }
  }
}
```

That's the entire setup. **Every `ILogger<T>` in your application routes automatically.** No code changes to your services, no filter expressions, no sub-loggers.

---

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| [`CSharpEssentials.LoggerHelper`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) | Core routing engine · `ILogger<T>` provider · JSON & Fluent API | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg?color=blue)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) |
| [`...Sink.Console`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) | Colored console · per-level themes | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Console.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) |
| [`...Sink.File`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) | Rolling JSON files · configurable retention | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.File.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) |
| [`...Sink.Email`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) | SMTP alerts · HTML templates · throttling | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Email.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) |
| [`...Sink.Telegram`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) | Bot notifications · MarkdownV2 · throttling | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Telegram.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) |
| [`...Sink.Elasticsearch`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) | Elasticsearch / OpenSearch indexing · Kibana-ready | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Elasticsearch.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) |
| [`...Sink.MSSqlServer`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) | SQL Server · auto table creation · custom columns | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.MSSqlServer.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) |
| [`...Sink.Postgresql`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) | PostgreSQL · JSONB columns · custom schema | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Postgresql.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) |
| [`...Sink.Seq`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) | Seq centralized log server · search & alerting | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Seq.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) |
| [`...Sink.HangfireConsole`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) | Structured logs in Hangfire Dashboard · colored output | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.HangfireConsole.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) |
| [`CSharpEssentials.HttpHelper`](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) | HttpClient + Polly resilience · rate limiting · structured logging | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.HttpHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) |

---

## Quick Start — 30 Seconds

### Option A — JSON config (recommended for teams)

**Step 1.** Install the core and the sinks you need:

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
# add more sinks as needed — each is independent
```

**Step 2.** Register in `Program.cs` — 2 lines:

```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
app.UseLoggerHelper();
```

**Step 3.** Create `appsettings.LoggerHelper.json`:

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Debug", "Information", "Warning"] },
      { "Sink": "File",    "Levels": ["Information", "Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "File": { "Path": "Logs", "RollingInterval": "Day", "RetainedFileCountLimit": 30 }
    },
    "General": { "EnableRequestResponseLogging": true }
  }
}
```

**Step 4.** Use `ILogger<T>` exactly as before — nothing changes in your services:

```csharp
public class OrderService(ILogger<OrderService> logger)
{
    public async Task ProcessAsync(int orderId)
    {
        logger.LogInformation("Processing order {OrderId}", orderId);  // → Console + File
        logger.LogError("Payment failed for {OrderId}", orderId);      // → File only
    }
}
```

### Option B — Add an advanced sink (Email alerts for errors)

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Email
```

Add to your JSON — no other code changes:

```json
{
  "LoggerHelper": {
    "Routes": [
      { "Sink": "Console", "Levels": ["Debug", "Information", "Warning"] },
      { "Sink": "File",    "Levels": ["Information", "Warning", "Error", "Fatal"] },
      { "Sink": "Email",   "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "File":  { "Path": "Logs", "RollingInterval": "Day" },
      "Email": {
        "From": "alerts@myapp.com", "To": "ops@myapp.com",
        "Host": "smtp.gmail.com",   "Port": 587,
        "Username": "alerts@myapp.com", "Password": "app-password",
        "ThrottleInterval": "00:05:00"
      }
    }
  }
}
```

### Option C — Fluent API (for programmatic or test setups)

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
    .AddRoute("File",    LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .AddRoute("Email",   LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureFile(f  => { f.Path = "Logs"; f.RollingInterval = "Day"; })
    .ConfigureEmail(e => { e.To = "ops@myapp.com"; e.Host = "smtp.myapp.com"; })
    .EnableRequestResponseLogging()
);
```

### Option D — JSON + Fluent merge (for per-environment overrides)

```csharp
// JSON holds shared production config.
// Fluent adds Development-only extras without touching JSON.
builder.Services.AddLoggerHelper(builder.Configuration, b => b
    .AddRoute("Console", LogEventLevel.Debug)
);
```

---

## Feature Highlights

### Per-Level Sink Routing — Declarative

The core differentiator: each sink receives **only the log levels you assign to it**, declared in JSON without sub-loggers, filter expressions, or conditional code.

```json
"Routes": [
  { "Sink": "Console",       "Levels": ["Debug", "Information", "Warning"] },
  { "Sink": "File",          "Levels": ["Information", "Warning", "Error", "Fatal"] },
  { "Sink": "Telegram",      "Levels": ["Error", "Fatal"] },
  { "Sink": "Email",         "Levels": ["Fatal"] },
  { "Sink": "Elasticsearch", "Levels": ["Information", "Warning", "Error", "Fatal"] }
]
```

### Native `ILogger<T>` — Zero Code Changes

LoggerHelper registers as a standard `Microsoft.Extensions.Logging` provider. If your app already uses `ILogger<T>`, **you change nothing in your services**:

```csharp
public class PaymentService(ILogger<PaymentService> logger)
{
    public void Charge(decimal amount)
    {
        logger.LogInformation("Charging {Amount:C}", amount);   // → Console + File
        logger.LogError("Charge declined for {Amount:C}", amount); // → File + Email + Telegram
    }
}
```

Named message template parameters (`{Amount}`) are preserved as structured Serilog properties — never flattened into strings.

### `BeginScope` — Context That Travels

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> {
    ["OrderId"] = orderId,
    ["UserId"]  = userId
}))
{
    _logger.LogInformation("Validation started");   // OrderId + UserId on every event
    await ValidateStock();                           // propagated into called methods too
    _logger.LogInformation("Order confirmed");
}
```

### Automatic Enrichment

Every log event carries these properties with no configuration:

| Property | Value |
|----------|-------|
| `ApplicationName` | From config or `WithApplicationName()` |
| `MachineName` | `Environment.MachineName` |
| `SourceContext` | Class name from `ILogger<T>` |
| `TraceId` / `SpanId` | `System.Diagnostics.Activity` (OpenTelemetry-compatible) |

### Internal Diagnostics — Resilient by Default

If a sink fails (wrong connection string, unreachable SMTP, network timeout), **your app keeps running**. Failures are captured in an injectable store you can expose on a health endpoint:

```csharp
app.MapGet("/health/logging", (ILogErrorStore errors) =>
    errors.Count == 0
        ? Results.Ok("All sinks healthy")
        : Results.Problem(string.Join("\n", errors.GetAll()
            .Select(e => $"{e.SinkName}: {e.ErrorMessage}")))
);
```

### Request/Response Logging — One Setting

```json
"General": { "EnableRequestResponseLogging": true }
```
```csharp
app.UseLoggerHelper();
```

Full HTTP request/response logging with correlation IDs, timing, and structured JSON output — in two lines.

---

## Sink Overview

Each sink is a **separate NuGet package**. Install only what you need; unused sinks add zero overhead.

### Console
Colored output with per-level themes. No configuration required — add the route and go.

```json
"Routes": [{ "Sink": "Console", "Levels": ["Debug", "Information", "Warning"] }]
```

### File
Rolling JSON log files with configurable retention and size limits.

```json
"Sinks": {
  "File": { "Path": "Logs", "RollingInterval": "Day", "RetainedFileCountLimit": 7 }
}
```

### Email
HTML email alerts with SMTP, per-event templates, and built-in throttling to prevent alert storms.

```json
"Sinks": {
  "Email": {
    "From": "alerts@myapp.com", "To": "team@myapp.com",
    "Host": "smtp.gmail.com",   "Port": 587,
    "Username": "alerts@myapp.com", "Password": "app-password",
    "ThrottleInterval": "00:05:00"
  }
}
```

### Telegram
Instant bot notifications with MarkdownV2 formatting and emoji-coded severity levels.

```json
"Sinks": {
  "Telegram": { "BotToken": "123456:ABC-DEF...", "ChatId": "-100123456789" }
}
```

### Elasticsearch
Structured indexing with automatic index naming for Kibana dashboards and OpenSearch.

```json
"Sinks": {
  "Elasticsearch": { "NodeUris": "http://localhost:9200", "IndexFormat": "myapp-{0:yyyy.MM.dd}" }
}
```

### SQL Server
Persistent log storage with auto table creation and support for custom columns.

```json
"Sinks": {
  "MSSqlServer": {
    "ConnectionString": "Server=.;Database=Logs;Trusted_Connection=true",
    "TableName": "AppLogs", "AutoCreateSqlTable": true
  }
}
```

### PostgreSQL
JSONB columns, custom schema, auto table creation — production-ready out of the box.

```json
"Sinks": {
  "Postgresql": {
    "ConnectionString": "Host=localhost;Database=logs;Username=app;Password=secret",
    "TableName": "app_logs", "NeedAutoCreateTable": true
  }
}
```

### Seq
Centralized log server with full-text search, dashboards, and alerting.

```json
"Sinks": {
  "Seq": { "ServerUrl": "http://localhost:5341", "ApiKey": "your-api-key" }
}
```

### Hangfire Console
See structured, colored logs directly in the Hangfire Dashboard during job execution — without leaving your monitoring UI.

```csharp
builder.Services.AddHangfireConsoleSink();
```
```json
"Routes": [{ "Sink": "HangfireConsole", "Levels": ["Information", "Warning", "Error"] }]
```

---

## Comparison vs Pure Serilog & NLog

| Feature | Serilog (pure) | NLog | **LoggerHelper v5** |
|---------|:--------------:|:----:|:-------------------:|
| Per-level sink routing | Manual sub-loggers | `FilteringTargetWrapper` | **JSON / fluent — built-in** |
| `ILogger<T>` compatible | Via bridge package | Native | **Native — zero code change** |
| Modular NuGet sinks | Partial (manual config) | Yes | **Yes — auto-register via `[ModuleInitializer]`** |
| Named params preserved as structured | Yes | Yes | **Yes** |
| `BeginScope` propagated to sink | Yes | Yes | **Yes** |
| OpenTelemetry TraceId correlation | Manual | Via extension | **Built-in, auto-correlated** |
| Internal error diagnostics | No | No | **Yes — injectable `ILogErrorStore`** |
| Fluent OR JSON OR merged | Code only | XML + code | **All three, mergeable** |
| Request/Response middleware | `Serilog.AspNetCore` | Manual | **1 setting + 1 line** |
| Email/Telegram alerts | 3rd-party sinks | NLog.MailKit | **Built-in + throttling** |
| Source Generator registration | No | No | **Yes — AOT-compatible** |
| Sink routing overhead | Baseline | ~−5% | **< 5% over Serilog** |
| Setup complexity | 20–40 lines | XML + code | **2 lines + JSON** |

---

## Performance

Benchmarks run with [BenchmarkDotNet](https://benchmarkdotnet.org) on .NET 9 / Release build.
Reproduce locally: `dotnet run -c Release --project src/CSharpEssentials.LoggerHelper.Benchmarks -- --filter *`

| Scenario | Serilog (baseline) | NLog | **LoggerHelper v5** |
|---|:---:|:---:|:---:|
| Single log, Information | 1.00× | ~0.95× | **~1.02×** |
| Multi-sink routing (3 routes) | 1.00× | ~0.98× | **~1.05×** |
| Throughput (1M events, async) | ~2.1M ev/s | ~1.8M ev/s | **~2.0M ev/s** |
| Startup (DI + 3 sinks, JSON) | ~38 ms | — | **~45 ms** |

> LoggerHelper adds **under 5% routing overhead** vs Serilog for typical multi-sink setups. Startup overhead includes plugin discovery; with source-generated registration it is reduced further. Full results: [`docs/benchmarks.md`](docs/benchmarks.md).

---

## Coming Soon

Contributions are welcome — open an issue or PR to get involved.

| Feature | Status | Description |
|---------|--------|-------------|
| **LoggerHelper.Dashboard** | Planned | Embedded real-time UI showing active sinks, routing rules, and recent errors |
| **LoggerHelper.AI** | Planned | Natural-language log queries, anomaly detection, incident summarization via LLM |
| **LoggerHelper.Telemetry** | Planned | OpenTelemetry metrics export — log counters per sink, error rates, latency histograms |
| **LoggerHelper.xUnit** | Planned | Forwards log output to xUnit test runner for integration test visibility |
| **`dotnet new` template** | In progress | `dotnet new loggerhelper-api` scaffolds a pre-configured project in seconds |

---

## Architecture

LoggerHelper uses a **plugin architecture** with zero compile-time coupling between the core and any sink.

```
Your Application
  └── ILogger<T>  ──────────────────────────────────────────┐
                                                             ▼
  CSharpEssentials.LoggerHelper (core)
    ├── LoggerHelperProvider  (ILoggerProvider)
    ├── RoutingEngine         (routes by Level × Sink config)
    ├── ILogErrorStore        (captures sink failures silently)
    └── SinkPluginRegistry    (global auto-registration hub)
          ├── Sink.Console    ([ModuleInitializer] → auto-registers)
          ├── Sink.File       ([ModuleInitializer] → auto-registers)
          ├── Sink.Email      ([ModuleInitializer] → auto-registers)
          └── ... any ISinkPlugin (yours too)
```

Sinks self-register via `[ModuleInitializer]` — a .NET 5+ attribute that runs before `Main()`. The core never references sinks at compile time; the plugin registry is populated at runtime by whichever sink NuGets are installed.

### Building a Custom Sink

```csharp
// 1. Implement ISinkPlugin
[LoggerHelperSink]
public sealed class SlackSinkPlugin : ISinkPlugin
{
    public bool CanHandle(string sinkName) =>
        sinkName.Equals("Slack", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options)
    {
        var opts = options.GetSinkConfig<SlackOptions>("Slack")
                   ?? options.BindSinkSection<SlackOptions>("Slack");

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt  => wt.Slack(opts.WebhookUrl)
        );
    }
}

// 2. Self-register — the core discovers it at startup automatically
public static class PluginInitializer
{
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new SlackSinkPlugin());
}
```

Reference `CSharpEssentials.LoggerHelper` as a **NuGet package** (not a project reference). No changes to the core or to any other sink are required.

---

## Documentation & Links

| Resource | Link |
|----------|------|
| Documentation Site | [loggerhelper.com](https://www.loggerhelper.com) |
| Interactive Playground | [loggerhelper.com/playground.html](https://www.loggerhelper.com/playground.html) |
| Benchmark Results | [docs/benchmarks.md](docs/benchmarks.md) |
| Migration Guide (v2/v4 → v5) | [docs/legacy-parity-v5.md](docs/legacy-parity-v5.md) |
| Gap Analysis | [docs/gap-analysis-original-vs-new.md](docs/gap-analysis-original-vs-new.md) |
| NuGet — LoggerHelper | [nuget.org/packages/CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) |
| NuGet — HttpHelper | [nuget.org/packages/CSharpEssentials.HttpHelper](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) |
| Issues & Roadmap | [GitHub Issues](https://github.com/alexbypa/CSharp.Essentials/issues) |

---

## License

MIT — [Alessandro Chiodo](https://github.com/alexbypa)

[Website](https://www.loggerhelper.com) · [GitHub](https://github.com/alexbypa/CSharp.Essentials) · [NuGet](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) · [Issues](https://github.com/alexbypa/CSharp.Essentials/issues)
