<p align="center">
  <img src="img/CSharpEssentials.png" alt="CSharpEssentials Logo" width="120" />
</p>

<h1 align="center">CSharpEssentials — LoggerHelper</h1>

<p align="center">
  <strong>Stop writing Serilog boilerplate. Route any log level to any sink — in one JSON file.</strong>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper"><img src="https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg?label=NuGet&color=blue" alt="NuGet Version" /></a>
  <a href="https://www.nuget.org/packages/CSharpEssentials.LoggerHelper"><img src="https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg?label=downloads&color=brightgreen" alt="NuGet Downloads" /></a>
  <img src="https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-512BD4?logo=dotnet" alt=".NET Versions" />
  
  <a href="https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml"><img src="https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg" alt="CodeQL" /></a>
  <a href="https://github.com/alexbypa/CSharp.Essentials/actions/workflows/dependabot/dependabot-updates"><img src="https://github.com/alexbypa/CSharp.Essentials/actions/workflows/dependabot/dependabot-updates/badge.svg" alt="Dependabot Updates" /></a>
  
  <a href="https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License" /></a>
</p>
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

- [The Boilerplate Problem](#-the-boilerplate-problem)
- [Packages](#-packages)
- [Quick Start](#-quick-start)
- [Run the Demo in 60 Seconds](#-run-the-demo-in-60-seconds)
- [Feature Highlights](#-feature-highlights)
- [Sink Overview & JSON Examples](#-sink-overview--json-examples)
- [Comparison](#-comparison)
- [Architecture](#-architecture)
- [Coming Soon](#-coming-soon)
- [Documentation & Links](#-documentation--links)

---

## 🔥 The Boilerplate Problem

Setting up Serilog with multiple sinks, per-level routing, and enrichment means repeating the same
infrastructure code in every project. Here's what it typically looks like:

### ❌ Before — Vanilla Serilog (30+ lines, repeated per project)

```csharp
// Program.cs — Vanilla Serilog setup
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("ApplicationName", "MyApp")
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .Enrich.FromLogContext()
    .WriteTo.Conditional(
        e => e.Level is LogEventLevel.Information or LogEventLevel.Warning,
        cfg => cfg.Console())
    .WriteTo.Conditional(
        e => e.Level >= LogEventLevel.Information,
        cfg => cfg.File("Logs/log-.txt", rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7, shared: true,
                        formatter: new JsonFormatter()))
    .WriteTo.Conditional(
        e => e.Level >= LogEventLevel.Error,
        cfg => cfg.Email(new EmailConnectionInfo {
            FromEmail = "alerts@myapp.com",
            ToEmail   = "team@myapp.com",
            MailServer = "smtp.myapp.com",
            Port = 587,
            EnableSsl = true,
            EmailSubject = "[MyApp] Error"
        }))
    .CreateLogger();

builder.Host.UseSerilog();
// Repeat for every project. Adjust. Break on typos. Re-test from scratch.
```

### ✅ After — LoggerHelper (5 lines of C# + declarative JSON)

```csharp
// Program.cs — that's it
builder.Services.AddLoggerHelper(builder.Configuration);
app.UseLoggerHelper();
```

```jsonc
// appsettings.LoggerHelper.json
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
      "Email": { "From": "alerts@myapp.com", "To": "team@myapp.com",
                 "Host": "smtp.myapp.com", "Port": 587 }
    }
  }
}
```

**Every `ILogger<T>` in your app now routes through LoggerHelper. No other changes needed.**

---

## 📦 Packages

| Package | Description | Version |
|---------|-------------|---------|
| [`CSharpEssentials.LoggerHelper`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) | Core routing engine, `ILogger<T>` bridge, JSON/fluent config | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) |
| [`...Sink.Console`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) | Colored console output, per-level themes | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Console.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Console) |
| [`...Sink.File`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) | Rolling JSON files, per-property subdirectories, configurable retention | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.File.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.File) |
| [`...Sink.Email`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) | SMTP alerts, HTML templates, throttling | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Email.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email) |
| [`...Sink.Telegram`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) | Bot notifications, MarkdownV2, throttling | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Telegram.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Telegram) |
| [`...Sink.Elasticsearch`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) | Elasticsearch/OpenSearch indexing, Kibana-ready | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Elasticsearch.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Elasticsearch) |
| [`...Sink.MSSqlServer`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) | SQL Server structured logs, auto table creation | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.MSSqlServer.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MSSqlServer) |
| [`...Sink.Postgresql`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) | PostgreSQL, JSONB columns, custom schema | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Postgresql.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Postgresql) |
| [`...Sink.Seq`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) | Seq centralized log server | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Seq.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Seq) |
| [`...Sink.HangfireConsole`](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) | Structured logs in Hangfire Dashboard with color output | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.HangfireConsole.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole) |
| [`CSharpEssentials.HttpHelper`](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) | HttpClient + Polly resilience, rate limiting, auto logging | [![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.HttpHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.HttpHelper) |

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
      { "Sink": "Console", "Levels": ["Debug", "Information", "Warning"] },
      { "Sink": "File",    "Levels": ["Information", "Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "File": { "Path": "Logs", "RollingInterval": "Day", "RetainedFileCountLimit": 7 }
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

Clone the repo and start the interactive demo app — **no database required**, runs on Console + File only:

```bash
git clone https://github.com/alexbypa/CSharp.Essentials.git
cd CSharp.Essentials/src/CSharpEssentials.LoggerHelper.Demo
dotnet run
```

Open **[http://localhost:5000/swagger](http://localhost:5000/swagger)** — la Swagger UI mostra tutti gli scenari disponibili. Ogni endpoint produce log strutturati visibili immediatamente nel terminale e nella cartella `Logs/`.

> In modalità Development (`dotnet run` usa sempre Development), il progetto legge automaticamente `appsettings.LoggerHelper.debug.json` che configura solo **Console + File** — nessun SQL Server o PostgreSQL necessario.  
> Per attivare tutti e 4 i sink (Console, File, MSSqlServer, PostgreSQL) imposta `ASPNETCORE_ENVIRONMENT=Production`.

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

## 📋 Sink Overview & JSON Examples

Each sink is a separate NuGet package. Install only what you need.

<details>
<summary><strong>Console</strong> — colored terminal output</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
```

```jsonc
"Routes": [{ "Sink": "Console", "Levels": ["Debug", "Information", "Warning", "Error"] }]
// No Sinks.Console configuration required.
```
</details>

<details>
<summary><strong>File</strong> — rolling JSON files with optional per-property routing</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
```

```jsonc
"Routes": [{ "Sink": "File", "Levels": ["Information", "Warning", "Error", "Fatal"] }],
"Sinks": {
  "File": {
    "Path": "Logs",
    "RollingInterval": "Day",
    "RetainedFileCountLimit": 7,
    "Shared": true,
    "FileNameProperty": "TenantId"   // optional: routes to Logs/{TenantId}/log-.txt
  }
}
```
</details>

<details>
<summary><strong>Email</strong> — SMTP alerts with HTML templates and throttling</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Email
```

```jsonc
"Routes": [{ "Sink": "Email", "Levels": ["Error", "Fatal"] }],
"Sinks": {
  "Email": {
    "From": "alerts@myapp.com",
    "To": "team@myapp.com",
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "alerts@myapp.com",
    "Password": "YOUR_APP_PASSWORD",
    "EnableSsl": true,
    "ThrottleInterval": "00:05:00"   // max 1 email per 5 minutes
  }
}
```
</details>

<details>
<summary><strong>Telegram</strong> — instant bot notifications with MarkdownV2</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Telegram
```

```jsonc
"Routes": [{ "Sink": "Telegram", "Levels": ["Error", "Fatal"] }],
"Sinks": {
  "Telegram": {
    "BotToken": "123456789:ABC-DEFxxxxxxx",
    "ChatId": "-100123456789",
    "ThrottleInterval": "00:00:10"   // max 1 message per 10 seconds
  }
}
```
</details>

<details>
<summary><strong>Elasticsearch</strong> — full-text indexing, Kibana-ready</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Elasticsearch
```

```jsonc
"Routes": [{ "Sink": "Elasticsearch", "Levels": ["Information", "Warning", "Error", "Fatal"] }],
"Sinks": {
  "Elasticsearch": {
    "NodeUris": "http://localhost:9200",
    "IndexFormat": "myapp-{0:yyyy.MM.dd}",
    "Username": "elastic",           // optional: basic auth
    "Password": "YOUR_PASSWORD"
  }
}
```
</details>

<details>
<summary><strong>SQL Server</strong> — structured storage with auto table creation</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.MSSqlServer
```

```jsonc
"Routes": [{ "Sink": "MSSqlServer", "Levels": ["Warning", "Error", "Fatal"] }],
"Sinks": {
  "MSSqlServer": {
    "ConnectionString": "Server=.;Database=AppLogs;Trusted_Connection=true;TrustServerCertificate=true",
    "TableName": "Logs",
    "AutoCreateSqlTable": true
  }
}
```
</details>

<details>
<summary><strong>PostgreSQL</strong> — JSONB columns, custom schema</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Postgresql
```

```jsonc
"Routes": [{ "Sink": "Postgresql", "Levels": ["Warning", "Error", "Fatal"] }],
"Sinks": {
  "Postgresql": {
    "ConnectionString": "Host=localhost;Port=5432;Database=logs;Username=app;Password=secret",
    "TableName": "app_logs",
    "SchemaName": "public",
    "NeedAutoCreateTable": true
  }
}
```
</details>

<details>
<summary><strong>Seq</strong> — centralized log server with search and alerting</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Seq
```

```jsonc
"Routes": [{ "Sink": "Seq", "Levels": ["Debug", "Information", "Warning", "Error", "Fatal"] }],
"Sinks": {
  "Seq": {
    "ServerUrl": "http://localhost:5341",
    "ApiKey": "YOUR_SEQ_API_KEY"     // optional
  }
}
```
</details>

<details>
<summary><strong>Hangfire Console</strong> — structured logs inside Hangfire Dashboard</summary>

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.HangfireConsole
```

```csharp
// Required in Program.cs (in addition to AddLoggerHelper):
builder.Services.AddHangfireConsoleSink();
```

```jsonc
"Routes": [{ "Sink": "HangfireConsole", "Levels": ["Information", "Warning", "Error"] }]
// No Sinks.HangfireConsole configuration required.
```

```csharp
// In your Hangfire job:
public class MyJob(ILogger<MyJob> logger) {
    public void Execute() {
        logger.LogInformation("Job started");  // visible in Hangfire Dashboard
        // ...
        logger.LogInformation("Job completed");
    }
}
```
</details>

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

## 🔮 Coming Soon

| Feature | Description |
|---------|-------------|
| **LoggerHelper.AI** | Natural language log queries, anomaly detection, incident summarization via LLM |
| **LoggerHelper.Dashboard** | Embedded real-time UI — active sinks, routing rules, recent errors |
| **LoggerHelper.Telemetry** | OpenTelemetry metrics export — log counters per sink, error rates, latency |
| **LoggerHelper.xUnit** | Forwards log output to xUnit test runner for integration test visibility |
| **Source Generator** | Replace runtime reflection for sink loading — faster startup, AOT-compatible |
| **`dotnet new` template** | `dotnet new loggerhelper-api` scaffolds a pre-configured project |

Contributions welcome — open an issue or PR on [GitHub](https://github.com/alexbypa/CSharp.Essentials).

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