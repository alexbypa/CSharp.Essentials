<p align="center">
  <img src="img/CSharpEssentials.png" alt="CSharpEssentials Logo" width="120" />
</p>

<h1 align="center">CSharpEssentials.LoggerHelper</h1>

<p align="center">
  <strong>Stop writing Serilog boilerplate. Route every log level to every sink — in 5 lines of JSON.</strong>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper"><img src="https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg?label=version&color=blue&logo=nuget" alt="NuGet Version" /></a>
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper"><img src="https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg?label=downloads&color=brightgreen" alt="NuGet Downloads" /></a>
  <img src="https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-512BD4?logo=dotnet&logoColor=white" alt=".NET Versions" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License MIT" /></a>
  <a href="https://github.com/alexbypa/CSharp.Essentials/commits/main"><img src="https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?color=orange" alt="Last Commit" /></a>
  <a href="https://www.loggerhelper.com"><img src="https://img.shields.io/badge/docs-loggerhelper.com-blue?logo=bookstack" alt="Documentation" /></a>
</p>

---

> **LoggerHelper is a Serilog orchestrator** — not a replacement. Your existing `ILogger<T>` code stays untouched. You only add configuration that decides *which log levels go where*.

---

## Table of Contents

- [The Boilerplate Problem](#-the-boilerplate-problem)
- [Packages](#-packages)
- [Quick Start](#-quick-start--2-minutes)
- [Feature Highlights](#-feature-highlights)
- [Sink Overview](#-sink-overview)
- [Comparison](#-comparison)
- [Architecture](#-architecture)
- [Coming Soon](#-coming-soon)
- [Documentation & Links](#-documentation--links)

---

## The Boilerplate Problem

Every .NET project deserves clean, structured logs. But configuring Serilog manually turns `Program.cs` into a wall of boilerplate that nobody wants to maintain.

### Before — Raw Serilog (30+ lines, every project)

```csharp
// Program.cs — the painful way
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug || e.Level == LogEventLevel.Information)
        .WriteTo.Console(new JsonFormatter()))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Warning)
        .WriteTo.File("logs/warnings-.txt", rollingInterval: RollingInterval.Day))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
        .WriteTo.Email(new EmailConnectionInfo {
            FromEmail = "alerts@myapp.com",
            ToEmail = "team@myapp.com",
            MailServer = "smtp.gmail.com",
            Port = 587,
            // ... 8 more lines
        }))
    .CreateLogger();

builder.Host.UseSerilog();
// ... repeated per environment, per project, per team
```

### After — LoggerHelper v5 (5 lines of code + declarative JSON)

```csharp
// Program.cs — the clean way
builder.Services.AddLoggerHelper(builder.Configuration);
app.UseLoggerHelper();
// Done. Every ILogger<T> in your app is now fully configured.
```

```json
// appsettings.LoggerHelper.json — routing at a glance
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Debug", "Information"] },
      { "Sink": "File",    "Levels": ["Warning", "Error", "Fatal"] },
      { "Sink": "Email",   "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "File":  { "Path": "Logs", "RollingInterval": "Day" },
      "Email": { "To": "team@myapp.com", "Host": "smtp.gmail.com", "Port": 587 }
    }
  }
}
```

**The result:** readable config, no duplicated routing logic, no surprises when you add a new sink. Your `ILogger<T>` classes are **completely untouched**.

---

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| [`CSharpEssentials.LoggerHelper`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) | Core routing engine, `ILogger<T>` bridge, JSON/fluent config | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg?color=blue)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) |
| [`...Sink.Console`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) | Colored console output with per-level themes | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Console.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) |
| [`...Sink.File`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) | Rolling JSON files, retention, per-tenant subdirectories | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.File.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) |
| [`...Sink.Email`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) | SMTP alerts, HTML templates, throttling | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Email.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) |
| [`...Sink.Telegram`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) | Bot notifications, MarkdownV2, emoji-coded levels | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Telegram.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) |
| [`...Sink.Elasticsearch`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) | Auto-indexing, Kibana dashboards, OpenSearch compatible | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Elasticsearch.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) |
| [`...Sink.MSSqlServer`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) | SQL Server, auto table creation, custom columns | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.MSSqlServer.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) |
| [`...Sink.Postgresql`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) | JSONB columns, custom schema, auto table creation | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Postgresql.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) |
| [`...Sink.Seq`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) | Seq centralized log server with search & alerting | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Seq.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) |
| [`...Sink.HangfireConsole`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) | Structured logs in the Hangfire Dashboard | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.HangfireConsole.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) |
| [`CSharpEssentials.HttpHelper`](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) | HttpClient + Polly resilience, rate limiting | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.HttpHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) |

---

## Quick Start — 2 Minutes

### Step 1 — Install

```bash
# Core (required)
dotnet add package CSharpEssentials.LoggerHelper

# Pick your sinks — install only what you need
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.File

# Add more sinks any time — no core changes required
dotnet add package CSharpEssentials.LoggerHelper.Sink.Email
dotnet add package CSharpEssentials.LoggerHelper.Sink.Telegram
```

### Step 2 — Wire up (2 lines)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoggerHelper(builder.Configuration); // <-- one line
// ... your other services

var app = builder.Build();
app.UseLoggerHelper(); // <-- one line
app.Run();
```

### Step 3 — Configure routing (JSON)

Create `appsettings.LoggerHelper.json` alongside `appsettings.json`:

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApi",
    "Routes": [
      { "Sink": "Console", "Levels": ["Debug", "Information", "Warning"] },
      { "Sink": "File",    "Levels": ["Information", "Warning", "Error", "Fatal"] },
      { "Sink": "Email",   "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "File": {
        "Path": "Logs",
        "RollingInterval": "Day"
      },
      "Email": {
        "From": "alerts@myapp.com",
        "To": "ops@yourcompany.com",
        "Host": "smtp.gmail.com",
        "Port": 587,
        "Username": "alerts@myapp.com",
        "Password": "your-app-password",
        "ThrottleInterval": "00:05:00"
      }
    },
    "General": { "EnableRequestResponseLogging": true }
  }
}
```

> **Don't forget** to include the file in your build: add `"appsettings.LoggerHelper.json"` to `ConfigureAppConfiguration` or reference it via `AddJsonFile`.

> [!IMPORTANT]
> **Debug vs Release configuration files**
>
> LoggerHelper follows the same environment-based convention as `appsettings.json`:
>
> | Environment | File loaded |
> |-------------|-------------|
> | `Debug` (Development) | `appsettings.LoggerHelper.debug.json` |
> | `Release` (Production / Staging) | `appsettings.LoggerHelper.json` |
>
> When running under the `Debug` build configuration, **`appsettings.LoggerHelper.json` is NOT read** — only `appsettings.LoggerHelper.debug.json` is loaded.
> Make sure you have both files in your project if your logging configuration differs between environments.
>
> ```
> MyApp/
> ├── appsettings.json
> ├── appsettings.LoggerHelper.json        ← Production / Release
> └── appsettings.LoggerHelper.debug.json  ← Debug / Development
> ```

### Step 4 — Use (exactly as before)

```csharp
public class OrderService(ILogger<OrderService> logger)
{
    public async Task ProcessAsync(int orderId)
    {
        logger.LogInformation("Processing order {OrderId}", orderId); // → Console + File
        logger.LogError("Payment failed for {OrderId}", orderId);     // → File + Email
    }
}
```

**Zero changes to your application code.** Structured properties like `{OrderId}` propagate to every sink as first-class fields — not flattened strings.

### Option B — Fluent API (no JSON file)

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApi")
    .AddRoute("Console", LogEventLevel.Debug, LogEventLevel.Information)
    .AddRoute("File",    LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .AddRoute("Email",   LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureFile(f  => { f.Path = "Logs"; f.RollingInterval = "Day"; })
    .ConfigureEmail(e => { e.To = "ops@yourcompany.com"; e.Host = "smtp.gmail.com"; })
    .EnableRequestResponseLogging()
);
```

### Option C — JSON base + Fluent override (best for multi-environment)

```csharp
// JSON defines shared config.
// Fluent adds Dev-only extras without touching shared config.
builder.Services.AddLoggerHelper(builder.Configuration, b => b
    .AddRoute("Console", LogEventLevel.Debug) // Dev-only verbose console
);
```

---

## Feature Highlights

### Per-Level Sink Routing — Declarative, Not Imperative

Send different levels to different destinations without nesting `Filter.ByIncludingOnly`:

```json
"Routes": [
  { "Sink": "Console",       "Levels": ["Debug", "Information"] },
  { "Sink": "File",          "Levels": ["Information", "Warning", "Error", "Fatal"] },
  { "Sink": "Telegram",      "Levels": ["Error", "Fatal"] },
  { "Sink": "Email",         "Levels": ["Fatal"] },
  { "Sink": "Elasticsearch", "Levels": ["Information", "Warning", "Error", "Fatal"] }
]
```

Change routing in JSON — no recompile, no new deployment artifact.

### Native `ILogger<T>` — Zero Migration Cost

LoggerHelper plugs in as a standard `ILoggerProvider`. If your codebase already uses `ILogger<T>`, **nothing changes**:

```csharp
// This class is completely unaware of LoggerHelper.
// It works on day 1, it works after adding Telegram, it works after removing Email.
public class PaymentService(ILogger<PaymentService> logger) { ... }
```

### BeginScope — Correlation Context That Follows Every Log Line

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> {
    ["CorrelationId"] = correlationId,
    ["TenantId"]      = tenantId
}))
{
    _logger.LogInformation("Validation started");  // carries CorrelationId + TenantId
    await ValidateStockAsync();                     // logs inside also carry them
    _logger.LogInformation("Order confirmed");      // carries CorrelationId + TenantId
}
```

### Automatic Enrichment — Out of the Box

| Property | Source |
|----------|--------|
| `ApplicationName` | Config / `WithApplicationName()` |
| `MachineName` | `Environment.MachineName` |
| `SourceContext` | Class name from `ILogger<T>` |
| `TraceId` / `SpanId` | `System.Diagnostics.Activity` (OpenTelemetry) |

### Sink Failure Isolation — Your App Never Crashes Because of Logs

If SMTP is unreachable or Elasticsearch is down, your app keeps running. Query the error store at any health endpoint:

```csharp
app.MapGet("/health/logging", (ILogErrorStore errors) =>
    errors.Count == 0
        ? Results.Ok("All sinks healthy")
        : Results.Problem(string.Join("\n", errors.GetAll()
            .Select(e => $"{e.SinkName}: {e.ErrorMessage}")))
);
```

### Request/Response Logging Middleware — One Setting

```json
"General": { "EnableRequestResponseLogging": true }
```

```csharp
app.UseLoggerHelper();
```

Full HTTP request/response logging with correlation IDs, timing, and structured body capture — two lines total.

### Multi-Tenant File Routing (v5.0.1+)

```json
"Sinks": {
  "File": { "Path": "Logs", "RollingInterval": "Day", "FileNameProperty": "TenantId" }
}
```

Logs with `TenantId = "acme"` → `Logs/acme/log-.txt`. Logs without the property → `Logs/log-.txt`. Zero extra code.

---

## Sink Overview

Each sink is a **separate NuGet package**. Install only what you need — the core never changes.

| Sink | Best For | Key Config |
|------|----------|-----------|
| **Console** | Local dev, structured output | Themes, colors per level |
| **File** | Compliance, local archive | Rolling interval, retention, tenant routing |
| **Email** | Critical alert escalation | SMTP, HTML templates, throttle interval |
| **Telegram** | Real-time ops notifications | BotToken, ChatId, MarkdownV2 |
| **Elasticsearch** | Full-text search, Kibana | Index format, node URIs |
| **MSSqlServer** | Relational log storage | Auto table creation, custom columns |
| **Postgresql** | JSONB-native log storage | JSONB fields, custom schema |
| **Seq** | Centralized log server | Server URL, API key |
| **HangfireConsole** | Background job visibility | Logs appear inside Hangfire Dashboard |

### HangfireConsole — Structured Logs Inside Your Dashboard

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.HangfireConsole
dotnet add package Hangfire.Console
```

```csharp
// Program.cs
builder.Services.AddHangfireConsoleSink(); // registers the accessor DI service
```

```json
"Routes": [
  { "Sink": "HangfireConsole", "Levels": ["Information", "Warning", "Error"] }
]
```

```csharp
// In your Hangfire job — inject IHangfireConsoleAccessor
public class ReportJob(ILogger<ReportJob> logger, IHangfireConsoleAccessor accessor)
{
    public void Execute(PerformContext context)
    {
        accessor.Set(context);     // bind current job context
        logger.LogInformation("Generating monthly report"); // appears in Hangfire Dashboard
        // ... job logic
        accessor.Clear();          // always clear when done
    }
}
```

**Note:** `.UseConsole()` must be registered in your Hangfire server config. Supports .NET 6 / 8 / 9 / 10.

---

## Comparison

| Feature | Serilog (manual) | NLog | **LoggerHelper v5** |
|---------|:---:|:---:|:---:|
| Per-level sink routing — declarative | Sub-logger + Filter | FilteringTargetWrapper | **JSON / fluent — native** |
| `ILogger<T>` zero migration | Via bridge pkg | Native | **Native — zero code change** |
| Install only needed sinks | No | No | **Yes — modular NuGet** |
| Named structured params preserved | Yes | Yes | **Yes** |
| `BeginScope` → Serilog props | Yes | Partial | **Yes — full propagation** |
| OpenTelemetry trace correlation | Manual | Via extension | **Built-in, auto** |
| Sink failure isolation | No | No | **Yes — `ILogErrorStore`** |
| Config: JSON or fluent or both | No | No | **All three, mergeable** |
| Request/Response middleware | Serilog.AspNetCore | Manual | **1 line** |
| Email/Telegram alerts + throttle | 3rd-party | NLog.MailKit | **Built-in** |
| Multi-tenant file routing | Manual | Manual | **Built-in via property** |
| Setup lines (typical app) | 25–40 | XML + 15 lines | **5 lines** |

---

## Architecture

LoggerHelper is a **plugin-based Serilog orchestrator**. The core has zero compile-time dependencies on any sink — each sink self-registers at application startup via `[ModuleInitializer]`.

```
Your App
  └── CSharpEssentials.LoggerHelper   (core — always installed)
        ├── ILoggerProvider bridge    → ILogger<T> → Serilog pipeline
        ├── SinkPluginRegistry        ← sinks self-register here
        ├── Per-level router          → dispatches events by level
        └── Loaded sink plugins (auto-discovered)
              ├── Sink.Console        [ModuleInitializer] → auto-registers
              ├── Sink.File           [ModuleInitializer] → auto-registers
              ├── Sink.Email          [ModuleInitializer] → auto-registers
              └── Your custom sink    [ModuleInitializer] → auto-registers
```

### Building a Custom Sink in ~15 Lines

```csharp
[LoggerHelperSink]
public sealed class SlackSinkPlugin : ISinkPlugin
{
    public bool CanHandle(string sinkName) =>
        sinkName.Equals("Slack", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration cfg, SinkRouting routing, LoggerHelperOptions opts)
    {
        var options = opts.GetSinkConfig<SlackOptions>("Slack")
                   ?? opts.BindSinkSection<SlackOptions>("Slack");

        cfg.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt  => wt.Slack(options.WebhookUrl)
        );
    }
}

public static class PluginInitializer
{
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new SlackSinkPlugin());
}
```

Publish it as a NuGet package. Users install it, add one route in JSON — no changes to the core required, ever.

---

## Coming Soon

| Feature | Description |
|---------|-------------|
| **`dotnet new` template** | `dotnet new loggerhelper-api` scaffolds a pre-configured project — zero friction onboarding |
| **LoggerHelper.Dashboard** | Embedded real-time UI: active sinks, routing rules, recent errors |
| **LoggerHelper.Telemetry** | OpenTelemetry metrics — log counters per sink, error rates, latency histograms |
| **LoggerHelper.AI** | Natural language log queries, anomaly detection, incident summarization via LLM |
| **Source Generator (AOT)** | Compile-time sink registration — faster startup, trimming-safe, AOT-compatible |

---

## Documentation & Links

| Resource | URL |
|----------|-----|
| Documentation site | [loggerhelper.com](https://www.loggerhelper.com) |
| Interactive playground | [loggerhelper.com/playground.html](https://www.loggerhelper.com/playground.html) |
| Benchmark results | [docs/benchmarks.md](docs/benchmarks.md) |
| Migration guide (v2/v4 → v5) | [docs/legacy-parity-v5.md](docs/legacy-parity-v5.md) |
| Gap analysis | [docs/gap-analysis-original-vs-new.md](docs/gap-analysis-original-vs-new.md) |
| NuGet — LoggerHelper | [nuget.org/packages/CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) |
| NuGet — HttpHelper | [nuget.org/packages/CSharpEssentials.HttpHelper](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) |
| GitHub Issues | [github.com/alexbypa/CSharp.Essentials/issues](https://github.com/alexbypa/CSharp.Essentials/issues) |

---

## License

MIT — [Alessandro Chiodo](https://github.com/alexbypa)

[Website](https://www.loggerhelper.com) · [GitHub](https://github.com/alexbypa/CSharp.Essentials) · [NuGet](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) · [Issues](https://github.com/alexbypa/CSharp.Essentials/issues)
