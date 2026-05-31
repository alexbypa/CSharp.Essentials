<p align="center">
  <img src="CSharpEssentials.LoggerHelper/img/CSharpEssentials.png" alt="CSharpEssentials Logo" width="120" />
</p>

<h1 align="center">CSharpEssentials</h1>

<p align="center">
  <strong>Route Serilog sinks by log level — zero boilerplate, native <code>ILogger&lt;T&gt;</code></strong>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper"><img src="https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg?label=LoggerHelper&color=blue" alt="NuGet Version" /></a>
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper"><img src="https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg?label=downloads&color=green" alt="NuGet Downloads" /></a>
  <img src="https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-blue?logo=dotnet" alt=".NET Versions" />
  <a href="https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License" /></a>
  <a href="https://github.com/alexbypa/CSharp.Essentials/commits/main"><img src="https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials" alt="Last Commit" /></a>
</p>

---

Install the NuGet, add a few lines of JSON or fluent C#, and every `ILogger<T>` in your app automatically routes logs to **Console**, **File**, **Email**, **Telegram**, **Elasticsearch**, **SQL Server**, **PostgreSQL**, **Seq**, and **Hangfire Console** — each sink receiving only the log levels you choose.

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
```

---

## Table of Contents

- [Packages](#-packages)
- [Quick Start](#-quick-start--30-seconds)
- [Feature Highlights](#-feature-highlights)
- [Sink Overview](#-sink-overview)
- [Comparison](#-comparison)
- [Coming Soon](#-coming-soon)
- [Architecture](#-architecture)
- [Documentation & Links](#-documentation--links)

---

## Packages

| Package | Description | Version |
|---------|-------------|---------|
| [`CSharpEssentials.LoggerHelper`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) | Core routing engine, `ILogger<T>` bridge, JSON/fluent config | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg) |
| [`...Sink.Console`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) | Colored console output, per-level themes | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Console.svg) |
| [`...Sink.File`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) | Rolling JSON files, configurable retention | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.File.svg) |
| [`...Sink.Email`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) | SMTP alerts, HTML templates, throttling | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Email.svg) |
| [`...Sink.Telegram`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) | Bot notifications, MarkdownV2, throttling | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Telegram.svg) |
| [`...Sink.Elasticsearch`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) | Elasticsearch/OpenSearch indexing, Kibana | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Elasticsearch.svg) |
| [`...Sink.MSSqlServer`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) | SQL Server, auto table creation, custom columns | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.MSSqlServer.svg) |
| [`...Sink.Postgresql`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) | PostgreSQL, JSONB fields, custom schema | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Postgresql.svg) |
| [`...Sink.Seq`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) | Seq centralized log server | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Seq.svg) |
| [`...Sink.HangfireConsole`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) | Hangfire Dashboard console with colored output | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.HangfireConsole.svg) |
| [`CSharpEssentials.HttpHelper`](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) | HttpClient + Polly resilience, rate limiting | ![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.HttpHelper.svg) |

---

## Quick Start — 30 Seconds

### Option A — JSON config (recommended)

**`Program.cs`**
```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
app.UseLoggerHelper();
```

**`appsettings.LoggerHelper.json`**
```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning"] },
      { "Sink": "File",    "Levels": ["Information", "Warning", "Error", "Fatal"] },
      { "Sink": "Email",   "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "File":  { "Path": "Logs", "RollingInterval": "Day" },
      "Email": { "To": "ops@example.com", "Host": "smtp.example.com", "Port": 587 }
    },
    "General": { "EnableRequestResponseLogging": true }
  }
}
```

That's it. Every `ILogger<T>` in your app now routes through LoggerHelper.

### Option B — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
    .AddRoute("File",    LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .AddRoute("Email",   LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureFile(f => { f.Path = "Logs"; f.RollingInterval = "Day"; })
    .ConfigureEmail(e => { e.To = "ops@example.com"; e.Host = "smtp.example.com"; })
    .EnableRequestResponseLogging()
);
```

### Option C — JSON + fluent merge

```csharp
// JSON defines shared config across environments.
// Fluent adds Development-only extras without touching JSON.
builder.Services.AddLoggerHelper(builder.Configuration, b => b
    .AddRoute("Console", LogEventLevel.Debug)
);
```

---

## Feature Highlights

### Per-Level Sink Routing

Send different log levels to different destinations — declaratively:

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

If your app already uses `ILogger<T>`, you change **nothing**. LoggerHelper plugs in as a standard `ILoggerProvider`:

```csharp
public class OrderService(ILogger<OrderService> logger) {
    public void Process(int orderId) {
        logger.LogInformation("Processing order {OrderId}", orderId);  // -> Console + File
        logger.LogError("Payment failed for {OrderId}", orderId);      // -> File + Email
    }
}
```

Named parameters like `{OrderId}` are preserved as structured Serilog properties — not flattened into strings.

### BeginScope — Context That Travels

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> {
    ["OrderId"] = orderId,
    ["UserId"]  = userId
}))
{
    _logger.LogInformation("Validation started");   // OrderId + UserId attached
    await ValidateStock();                           // logs inside also carry them
    _logger.LogInformation("Order confirmed");       // OrderId + UserId attached
}
```

### Automatic Enrichment

Every log event automatically carries:

| Property | Source |
|----------|--------|
| `ApplicationName` | Config / `WithApplicationName()` |
| `MachineName` | `Environment.MachineName` |
| `SourceContext` | Class name from `ILogger<T>` |
| `TraceId` / `SpanId` | `System.Diagnostics.Activity` (OpenTelemetry) |

### Internal Diagnostics

If a sink fails (wrong connection string, unreachable SMTP), your app keeps running. Errors are captured silently:

```csharp
app.MapGet("/health/logging", (ILogErrorStore errors) =>
    errors.Count == 0
        ? Results.Ok("All sinks healthy")
        : Results.Problem(string.Join("\n", errors.GetAll().Select(e => $"{e.SinkName}: {e.ErrorMessage}")))
);
```

### Request/Response Logging Middleware

```json
"General": { "EnableRequestResponseLogging": true }
```
```csharp
app.UseLoggerHelper();
```

One setting, one line — full HTTP request/response logging with correlation IDs and timing.

---

## Sink Overview

Each sink is a separate NuGet package. Install only what you need.

### Console

Colored console output with per-level color themes.

```json
"Sinks": { }
```
No configuration required — just add the route.

### File

Rolling JSON log files with configurable retention.

```json
"Sinks": {
  "File": { "Path": "Logs", "RollingInterval": "Day", "RetainedFileCountLimit": 7 }
}
```

### Email

HTML email alerts with SMTP, templates, and throttling.

```json
"Sinks": {
  "Email": {
    "From": "alerts@myapp.com", "To": "team@myapp.com",
    "Host": "smtp.gmail.com", "Port": 587,
    "Username": "alerts@myapp.com", "Password": "app-password",
    "ThrottleInterval": "00:05:00"
  }
}
```

### Telegram

Instant bot notifications with MarkdownV2 and emoji-coded levels.

```json
"Sinks": {
  "Telegram": { "BotToken": "123456:ABC-DEF...", "ChatId": "-100123456789" }
}
```

### Elasticsearch

Full-text search with auto-indexing for Kibana dashboards.

```json
"Sinks": {
  "Elasticsearch": { "NodeUris": "http://localhost:9200", "IndexFormat": "myapp-{0:yyyy.MM.dd}" }
}
```

### SQL Server

Structured storage with auto table creation and custom columns.

```json
"Sinks": {
  "MSSqlServer": {
    "ConnectionString": "Server=.;Database=Logs;Trusted_Connection=true",
    "TableName": "AppLogs", "AutoCreateSqlTable": true
  }
}
```

### PostgreSQL

JSONB columns, custom schema, auto table creation.

```json
"Sinks": {
  "Postgresql": {
    "ConnectionString": "Host=localhost;Database=logs;Username=app;Password=secret",
    "TableName": "app_logs", "NeedAutoCreateTable": true
  }
}
```

### Seq

Centralized log server with search, dashboards, and alerting.

```json
"Sinks": {
  "Seq": { "ServerUrl": "http://localhost:5341", "ApiKey": "your-api-key" }
}
```

### Hangfire Console

See structured logs directly on the Hangfire Dashboard during job execution.

```csharp
// Extra DI registration required:
builder.Services.AddHangfireConsoleSink();
```
```json
"Routes": [
  { "Sink": "HangfireConsole", "Levels": ["Information", "Warning", "Error"] }
]
```

---

## Comparison

| Feature | Serilog alone | NLog | **LoggerHelper v5** |
|---------|:---:|:---:|:---:|
| Per-level sink routing (declarative) | Manual per sink | Via targets | **JSON / fluent — built-in** |
| `ILogger<T>` compatible | Via bridge pkg | Native | **Native — zero code change** |
| Install only needed sinks | No | No | **Yes — modular NuGet** |
| Named params preserved | Yes | Yes | **Yes** |
| `BeginScope` structured | Yes | Yes | **Yes — propagates to Serilog** |
| OpenTelemetry trace ID | Manual | Manual | **Built-in, auto-correlated** |
| Internal error diagnostics | No | No | **Yes — injectable ILogErrorStore** |
| Fluent OR JSON OR both | No | No | **All three, mergeable** |
| Request/Response middleware | Serilog.AspNetCore | Manual | **1 line middleware** |
| Email/Telegram alerts | 3rd-party sinks | NLog.MailKit | **Built-in + throttling** |
| Setup complexity | 15-30 lines | XML + code | **5 lines** |

---

## Coming Soon

These features are planned for upcoming releases — contributions welcome!

| Feature | Description |
|---------|-------------|
| **LoggerHelper.AI** | Natural language log queries, anomaly detection, incident summarization via LLM |
| **LoggerHelper.Dashboard** | Embedded real-time UI showing active sinks, routing rules, and recent errors |
| **LoggerHelper.Telemetry** | OpenTelemetry metrics export — log counters per sink, error rates, latency |
| **LoggerHelper.xUnit** | Forwards log output to xUnit test runner for integration test visibility |
| **Source Generator** | Replace runtime reflection for sink loading — faster startup, AOT-compatible, trimming-safe |
| **`dotnet new` template** | `dotnet new loggerhelper-api` scaffolds a pre-configured project with zero friction |

---

## Architecture

LoggerHelper uses a **plugin architecture** for sinks. The core package has zero dependencies on any sink — they self-register at startup via `[ModuleInitializer]`.

```
Your App
  └── CSharpEssentials.LoggerHelper (core)
        ├── Routes logs by level
        ├── Bridges ILogger<T> → Serilog
        └── Discovers sink plugins automatically
              ├── Sink.Console    (auto-registers)
              ├── Sink.File       (auto-registers)
              ├── Sink.Email      (auto-registers)
              └── ... any ISinkPlugin
```

### Building a Custom Sink

```csharp
[LoggerHelperSink]
public sealed class MyTargetSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        sinkName.Equals("MyTarget", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<MyTargetOptions>("MyTarget")
                   ?? options.BindSinkSection<MyTargetOptions>("MyTarget");

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.MySink(opts.ConnectionString)
        );
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new MyTargetSinkPlugin());
}
```

Reference `CSharpEssentials.LoggerHelper` as a NuGet package — not a project reference. The sink auto-registers at startup with no changes to the core.

---

## Documentation & Links

- [Documentation Site](https://www.loggerhelper.com)
- [Interactive Playground](https://www.loggerhelper.com/playground.html)
- [Benchmark Results](docs/benchmarks.md)
- [Migration Guide (v2/v4 to v5)](docs/legacy-parity-v5.md)
- [Gap Analysis](docs/gap-analysis-original-vs-new.md)
- [NuGet — LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [NuGet — HttpHelper](https://www.nuget.org/packages/CSharpEssentials.HttpHelper)

---

## License

MIT — [Alessandro Chiodo](https://github.com/alexbypa)

[GitHub](https://github.com/alexbypa/CSharp.Essentials) | [NuGet](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) | [Issues](https://github.com/alexbypa/CSharp.Essentials/issues)
