![CSharpEssentials Logo](img/CSharpEssentials.ico)
# CSharpEssentials — LoggerHelper

**Stop writing Serilog boilerplate. Route any log level to any sink — in one JSON file.**

[![NuGet Version](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg?label=NuGet&color=blue)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg?label=downloads&color=brightgreen)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
![.NET Versions](https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-512BD4?logo=dotnet)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)

[![Build and Test (src v5)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/build-test.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/build-test.yml)
[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/security/code-scanning)
[![Dependabot Updates](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/dependabot/dependabot-updates/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/dependabot/dependabot-updates)

[![GitHub stars](https://img.shields.io/github/stars/alexbypa/CSharp.Essentials?style=social)](https://github.com/alexbypa/CSharp.Essentials/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/alexbypa/CSharp.Essentials)](https://github.com/alexbypa/CSharp.Essentials/issues)

> ⭐ **If you find this project useful, please consider giving it a star on [GitHub](https://github.com/alexbypa/CSharp.Essentials)!**  
> 💡 *Explore the source code, check open issues, or contribute.*
---

**LoggerHelper** is a modular logging infrastructure for .NET. Install the core + only the sink packages you need, drop in a JSON config, and your entire app's `ILogger<T>` routes to Console, File, Email, Telegram, SQL Server, PostgreSQL, Elasticsearch, Seq, and Hangfire Console — each receiving only the log levels you configure.

**Zero code changes required** if you already use `ILogger<T>`. LoggerHelper registers as a standard `ILoggerProvider`.

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
```

---

## Table of Contents

- [Quick Start](#-quick-start)
- [Run the Demo in 60 Seconds](#-run-the-demo-in-60-seconds)
- [Why choose LoggerHelper?](#-why-choose-loggerhelper)
- [Packages](#-packages)
- [Feature Highlights](#-feature-highlights)
- [Built-in UI — Dashboard](#-built-in-ui--dashboard)
- [AI Integration — MCP Server](#-ai-integration--mcp-server-new-v510)
- [Sink Overview & JSON Examples](#-sink-overview--json-examples)
- [Comparison](#-comparison)
- [Architecture](#-architecture)
- [Coming Soon](#-coming-soon)
- [View Source & Contribute](#-view-source--contribute)
- [Documentation & Links](#-documentation--links)

---

## 🚀 Quick Start

### Option A — JSON config (recommended)

**1. Install packages**

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
```

**2. Wire up in `Program.cs`**

```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
app.UseLoggerHelper();
```

**3. Create `appsettings.LoggerHelper.json`** in your project root

```jsonc
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning"] },
      { "Sink": "File", "Levels": ["Information", "Warning", "Error", "Fatal"] },
      { "Sink": "Email", "Levels": ["Error", "Fatal"] },
      { "Sink": "MSSqlServer", "Levels": ["Information", "Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "File": {
        "Path": "Logs",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 7,
        "FileNameProperty": "TenantId"
      },
      "Email": {
        "From": "alerts@myapp.com",
        "To": "team@myapp.com",
        "Host": "smtp.myapp.com",
        "Port": 587
      },
      "MSSqlServer": {
        "ConnectionString": "Server=localhost;Database=LogsDb;Trusted_Connection=true;",
        "TableName": "Logs",
        "AutoCreateSqlTable": true
      }
    },
    "General": { "EnableRequestResponseLogging": true }
  }
}
```

Done. Every `ILogger<T>` in your app now routes through LoggerHelper.

---

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

### Option C — JSON + Fluent merge

```csharp
// JSON defines shared config across environments.
// Fluent adds development-only extras without touching JSON.
builder.Services.AddLoggerHelper(builder.Configuration, b => b
    .AddRoute("Console", LogEventLevel.Debug)
);
```

---

## ⚡ Run the Demo in 60 Seconds

Clone the repo and start the interactive demo app. The demo comes pre-configured with Console, File, MSSqlServer, and PostgreSQL sinks.

```bash
git clone https://github.com/alexbypa/CSharp.Essentials.git
cd CSharp.Essentials/src/CSharpEssentials.LoggerHelper.Demo
dotnet run
```

Open the URL shown in your terminal (usually **`http://localhost:<port>/swagger/index.html`**) — the Swagger UI lists all available demo scenarios. Each endpoint produces structured logs visible immediately in the terminal and in the `Logs/` folder.

> 💡 **No database required to run it:** Even if you don't have SQL Server or PostgreSQL running locally, LoggerHelper gracefully ignores the connection errors. Your app won't crash, and logs will still appear perfectly in the Console and File sinks!

---

## 🎯 Why choose LoggerHelper?

- 🔌 **Native `ILogger<T>`**: Zero vendor lock-in. Uses standard Microsoft abstractions.
- 🧩 **Zero Unnecessary Dependencies**: Highly modular. Install the core package and *only* the specific sinks you need.
- 🛠️ **JSON-First Configuration**: Route any log level to any sink without writing a single line of C# conditional logic.
- 🛡️ **PII Data Masking**: Automatically redact passwords, JWTs, and sensitive fields across *all* sinks globally.
- 🤖 **AI-Ready**: Includes a native MCP Server to let AI assistants query log health and configuration out-of-the-box.

---

## 📦 Packages

| Package | Description | Version |
|---------|-------------|---------|
| [`CSharpEssentials.LoggerHelper`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) | Core routing engine, `ILogger<T>` bridge, JSON/fluent config | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) |
| [`...Sink.Console`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) | Colored console output, per-level themes — [guide →](src/CSharpEssentials.LoggerHelper.Sink.Console/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Console.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) |
| [`...Sink.File`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) | Rolling JSON files, per-property subdirectories, configurable retention — [guide →](src/CSharpEssentials.LoggerHelper.Sink.File/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.File.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) |
| [`...Sink.Email`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) | SMTP alerts, HTML templates, throttling — [guide →](src/CSharpEssentials.LoggerHelper.Sink.Email/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Email.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) |
| [`...Sink.Telegram`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) | Bot notifications, MarkdownV2, throttling — [guide →](src/CSharpEssentials.LoggerHelper.Sink.Telegram/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Telegram.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) |
| [`...Sink.Elasticsearch`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) | Elasticsearch/OpenSearch indexing, Kibana-ready — [guide →](src/CSharpEssentials.LoggerHelper.Sink.Elasticsearch/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Elasticsearch.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) |
| [`...Sink.MSSqlServer`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) | SQL Server structured logs, auto table creation — [guide →](src/CSharpEssentials.LoggerHelper.Sink.MSSqlServer/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.MSSqlServer.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) |
| [`...Sink.Postgresql`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) | PostgreSQL, JSONB columns, custom schema — [guide →](src/CSharpEssentials.LoggerHelper.Sink.Postgresql/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Postgresql.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) |
| [`...Sink.Seq`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) | Seq centralized log server — [guide →](src/CSharpEssentials.LoggerHelper.Sink.Seq/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Seq.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) |
| [`...Sink.HangfireConsole`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) | Structured logs in Hangfire Dashboard with color output — [guide →](src/CSharpEssentials.LoggerHelper.Sink.HangfireConsole/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.HangfireConsole.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) |
| [`...Sink.Dashboard`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Dashboard) | Dashboard LoggerHelper — [guide →](src/CSharpEssentials.LoggerHelper.Dashboard/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Dashboard.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Dashboard) |
| [`CSharpEssentials.LoggerHelper.MCP`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.MCP) | MCP server: AI assistants can query sink health, errors & config — [guide →](src/CSharpEssentials.LoggerHelper.MCP/README.md) | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.MCP.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.MCP) |
| [`CSharpEssentials.HttpHelper`](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) | HttpClient + Polly resilience, rate limiting, auto logging | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.HttpHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) |


---

## ✨ Feature Highlights

### Per-Level Sink Routing — Declarative

Send different log levels to different destinations without writing conditional predicates:

```jsonc
"Routes": [
  { "Sink": "Console",       "Levels": ["Debug", "Information", "Warning"] },
  { "Sink": "File",          "Levels": ["Information", "Warning", "Error", "Fatal"] },
  { "Sink": "Telegram",      "Levels": ["Error", "Fatal"] },
  { "Sink": "Email",         "Levels": ["Fatal"] },
  { "Sink": "Elasticsearch", "Levels": ["Information", "Warning", "Error", "Fatal"] }
]
```

### Native `ILogger<T>` — Zero Code Changes

If your app already uses `ILogger<T>`, you change **nothing**. LoggerHelper registers as a standard `ILoggerProvider`:

```csharp
public class OrderService(ILogger<OrderService> logger) {
    public void Process(int orderId) {
        logger.LogInformation("Processing order {OrderId}", orderId);  // → Console + File
        logger.LogError("Payment failed for {OrderId}", orderId);      // → File + Email
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
    await ValidateStock();                           // inner logs also carry them
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

If a sink fails (wrong connection string, unreachable SMTP), your app keeps running. Errors are captured silently and inspectable at runtime:

```csharp
app.MapGet("/health/logging", (ILogErrorStore errors) =>
    errors.Count == 0
        ? Results.Ok("All sinks healthy")
        : Results.Problem(string.Join("\n", errors.GetAll().Select(e => $"{e.SinkName}: {e.ErrorMessage}")))
);
```

### Request/Response Logging Middleware

```jsonc
"General": { "EnableRequestResponseLogging": true }
```
```csharp
app.UseLoggerHelper();
```

One setting, one line — full HTTP request/response logging with correlation IDs and timing.

### Dynamic File Routing (Multi-Tenant)

Route logs to subdirectories based on any log property:

```jsonc
"Sinks": {
  "File": { "Path": "Logs", "RollingInterval": "Day", "FileNameProperty": "TenantId" }
}
```

Logs with `TenantId = "acme"` → `Logs/acme/log-20250101.txt`.  
Logs without the property → `Logs/log-20250101.txt`.

### Sensitive Data Masking — One JSON Block Protects Every Sink

Stop writing `Regex.Replace` calls before every `logger.LogInformation`. Declare what's sensitive
once, and LoggerHelper redacts it everywhere — Console, File, SQL, Elasticsearch, Seq, Telegram —
**before any sink ever sees it**:

```jsonc
"SensitiveDataMasking": {
  "Enabled": true,
  "MaskText": "***MASKED***",
  "Presets": [ "Email", "CreditCard", "JwtToken", "BearerToken", "ConnectionStringSecret" ],
  "SensitiveProperties": [ "Password", "ApiKey" ],
  "Rules": [
    { "Name": "OrderId", "Pattern": "ORD-\\d+" }
  ]
}
```

```csharp
logger.LogInformation("Login for {Email} with {Password}", "alice@example.com", "Sup3rSecret!");
// → every sink receives: Login for ***MASKED*** with ***MASKED***
```

- **Built-in presets** for the secrets that leak most often: emails, credit card numbers, JWTs,
  `Bearer ...` tokens, and `Password=...` / `Pwd=...` in connection strings.
- **`SensitiveProperties`** redacts named structured fields outright (e.g. `Password`, `ApiKey`),
  regardless of content.
- **Custom regex rules** with an optional `secret` capture group mask only part of a match —
  `Bearer ***MASKED***` keeps the scheme visible while hiding the token.
- **Zero overhead when disabled** (the default) — the enricher isn't added to the pipeline at all.

Serilog has no first-class equivalent: redaction usually means a hand-rolled `IDestructuringPolicy`
or a third-party enricher wired up per project. Here it's one JSON block, applied globally.

---

## 📊 Built-in UI — Dashboard

```bash
dotnet add package CSharpEssentials.LoggerHelper.Dashboard
```

Embedded real-time diagnostics dashboard for ASP.NET Core — **no Seq, no Kibana, no extra infrastructure required.**
Navigate to `/loggerhelper` to see the health of your logging pipeline at a glance:

- 📡 **Live Log Stream:** Browser-based `tail -f` via Server-Sent Events. Filter by level or text in real time.
- 🩺 **Sink Health Cards:** At-a-glance status for every configured sink (ACTIVE / FAILED) and their assigned log levels.
- ⚠️ **Error History:** Click-to-expand table of recent sink errors (SMTP failures, DB write errors) with full stack traces.
- ⏪ **Context Before Error:** The killer feature. A zero-allocation ring buffer automatically flushes all preceding Debug/Info/Warning entries *only* when an Error or Fatal event fires. You see **exactly what happened before the crash** without keeping verbose logging on permanently!

```csharp
// Program.cs
builder.Services.AddLoggerHelperDashboard();
// ...
app.MapLoggerHelperDashboard(); // Exposes the UI at /loggerhelper
```

---

## 🤖 AI Integration — MCP Server

```bash
dotnet add package CSharpEssentials.LoggerHelper.MCP
```

Give your AI assistant live visibility into your running app's logging state. Two lines of setup
expose a [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that Claude,
Cursor, GitHub Copilot, and any MCP-compatible client can query.

```csharp
// Program.cs — add after AddLoggerHelper()
builder.Services.AddLoggerHelperMcp();
// ...
app.MapLoggerHelperMcp("/mcp");        // Streamable HTTP  — Claude Code, Cursor, Copilot
app.MapLoggerHelperMcpSse();           // HTTP+SSE         — Claude Desktop, MCP Inspector
```

**AI can now ask your app:**
- *"Are all sinks healthy?"* → `loggerhelper_get_health` (OK / WARNING / CRITICAL)
- *"Show me the last 10 logging errors"* → `loggerhelper_get_errors`
- *"What levels does the Email sink receive?"* → `loggerhelper_get_sinks`
- *"Is PII masking enabled?"* → `loggerhelper_get_config`

**Predefined prompt** — run a full diagnostic with one command:

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"prompts/get","params":{"name":"diagnose-logging","arguments":{"focus":"all"}}}'
```

The `diagnose-logging` prompt instructs the AI to call all four tools and return a structured report
with **Overall Status**, **Failed Sinks**, **Configuration Issues**, and **Recommended Actions**.

**Why this matters:** Every other .NET logging library requires a separate dashboard (Seq, Kibana,
Grafana) before an AI assistant can see log state. LoggerHelper MCP ships that built in —
zero extra infrastructure, zero extra dependencies, one NuGet package.

---

## 📋 Sink Overview

Each sink is a separate NuGet package — install only what you need.
Click **guide →** for the full configuration reference, sample output, and troubleshooting for each sink.

| Sink | What it does | Install | Full guide |
|---|---|---|---|
| **Console** | Colored terminal output, per-level themes | `dotnet add package CSharpEssentials.LoggerHelper.Sink.Console` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.Console/README.md) |
| **File** | Rolling JSON files, per-property subdirectories, configurable retention | `dotnet add package CSharpEssentials.LoggerHelper.Sink.File` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.File/README.md) |
| **Email** | SMTP alerts, HTML templates, throttling | `dotnet add package CSharpEssentials.LoggerHelper.Sink.Email` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.Email/README.md) |
| **Telegram** | Bot notifications, MarkdownV2, throttling | `dotnet add package CSharpEssentials.LoggerHelper.Sink.Telegram` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.Telegram/README.md) |
| **Elasticsearch** | Elasticsearch / OpenSearch indexing, auto template, Kibana-ready | `dotnet add package CSharpEssentials.LoggerHelper.Sink.Elasticsearch` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.Elasticsearch/README.md) |
| **SQL Server** | Structured log table, auto creation, custom columns | `dotnet add package CSharpEssentials.LoggerHelper.Sink.MSSqlServer` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.MSSqlServer/README.md) |
| **PostgreSQL** | JSONB properties column, custom schema | `dotnet add package CSharpEssentials.LoggerHelper.Sink.Postgresql` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.Postgresql/README.md) |
| **Seq** | Centralized log server with search, alerts, and dashboards | `dotnet add package CSharpEssentials.LoggerHelper.Sink.Seq` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.Seq/README.md) |
| **Hangfire Console** | Structured logs inside the Hangfire Dashboard | `dotnet add package CSharpEssentials.LoggerHelper.Sink.HangfireConsole` | [guide →](src/CSharpEssentials.LoggerHelper.Sink.HangfireConsole/README.md) |

Every sink uses the same routing pattern — just add the sink name to `Routes`:

```jsonc
"Routes": [
  { "Sink": "Console",       "Levels": ["Debug", "Information", "Warning"] },
  { "Sink": "File",          "Levels": ["Information", "Warning", "Error", "Fatal"] },
  { "Sink": "Elasticsearch", "Levels": ["Warning", "Error", "Fatal"] }
]
```

For per-sink configuration options (connection strings, paths, retention, custom columns), see the **guide →** linked in the table above.

---

## 📊 Comparison

| Feature | Serilog alone | NLog | **LoggerHelper v5** |
|---------|:---:|:---:|:---:|
| Per-level sink routing (declarative) | Manual per sink | Via targets | **JSON / fluent — built-in** |
| `ILogger<T>` compatible | Via bridge pkg | Native | **Native — zero code change** |
| Install only needed sinks | ❌ | ❌ | **✅ modular NuGet** |
| Named params preserved (structured) | ✅ | ✅ | **✅** |
| `BeginScope` structured | ✅ | ✅ | **✅ propagates to Serilog** |
| OpenTelemetry trace correlation | Manual | Manual | **✅ built-in, auto** |
| Internal error diagnostics | ❌ | ❌ | **✅ injectable `ILogErrorStore`** |
| Fluent OR JSON OR merged | ❌ | ❌ | **✅ all three** |
| Request/Response middleware | Serilog.AspNetCore | Manual | **✅ 1 line** |
| Email/Telegram alerts | 3rd-party | NLog.MailKit | **✅ built-in + throttling** |
| Dynamic file routing by property | ❌ | ❌ | **✅ multi-tenant ready** |
| Sink plugin system (custom sinks) | Manual wiring | Manual | **✅ `[ModuleInitializer]` auto-reg** |
| Initial setup complexity | 15–30 lines | XML + code | **✅ 5 lines** |
| Sensitive data masking (PII/secrets) | Manual `IDestructuringPolicy` | 3rd-party | **✅ JSON-driven, all sinks at once** |

---

## 🏗️ Architecture

LoggerHelper uses a **zero-dependency plugin architecture**. The core package has no knowledge of any specific sink — they self-register at startup via `[ModuleInitializer]`.

```
Your App
  └── CSharpEssentials.LoggerHelper (core)
        ├── Bridges ILogger<T> → Serilog (zero allocations on hot path)
        ├── Routes events by level via pre-computed HashSet<LogEventLevel>
        ├── Exposes ILogErrorStore for sink failure diagnostics
        └── Discovers sink plugins automatically at startup
              ├── Sink.Console    (auto-registers via [ModuleInitializer])
              ├── Sink.File       (auto-registers)
              ├── Sink.Email      (auto-registers)
              └── ... any ISinkPlugin
```

### Performance Focus

Every version ships a targeted performance audit. Key hot-path optimizations to date:

| Version | Component | Optimization | Impact |
|---------|-----------|-------------|--------|
| v5.0.6 | Middleware `ReadBodySafe` | `ArrayPool<char>` replaces `new char[64K]` | −256 KB LOH per request |
| v5.0.5 | `SinkRouting.Matches()` | `HashSet<LogEventLevel>` replaces string O(n) scan | Zero alloc per log event |
| v5.0.5 | Telegram `Emit()` | Fire-and-forget `Task.Run` vs blocking `GetResult()` | No pipeline stall |
| v5.0.5 | Email template | Cached at ctor vs `File.ReadAllText` per emit | No disk I/O on hot path |

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
            wt => wt.MySink(opts?.ConnectionString)
        );
    }
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new MyTargetSinkPlugin());
}
```

Reference `CSharpEssentials.LoggerHelper` as a NuGet package. The sink auto-registers with no changes to the core.

---

## 🤝 View Source & Contribute

If you're reading this on **NuGet**, we highly recommend visiting our **[GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)**!

By visiting the repository you can:
- 👀 **Explore the source code** and evaluate the code quality.
- 🐛 **Check open issues** or report new ones.
- 🛠️ **Contribute** to the project via Pull Requests.
- ⭐ **Drop a star** to support the project's growth!

---

## 📚 Documentation & Links

- [**Documentation Site**](https://www.loggerhelper.com) — full reference, guides, and playground
- [**Interactive Playground**](https://www.loggerhelper.com/playground.html)
- [**Changelog**](CHANGELOG.md)
- [**Benchmark Results**](docs/benchmarks.md)
- [**Migration Guide v2/v4 → v5**](docs/legacy-parity-v5.md)
- [**NuGet — LoggerHelper**](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [**NuGet — HttpHelper**](https://www.nuget.org/packages/CSharpEssentials.HttpHelper)
- [**GitHub Issues**](https://github.com/alexbypa/CSharp.Essentials/issues)

---

## License

MIT — [Alessandro Chiodo](https://github.com/alexbypa)

[GitHub](https://github.com/alexbypa/CSharp.Essentials) · [NuGet](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) · [loggerhelper.com](https://www.loggerhelper.com)