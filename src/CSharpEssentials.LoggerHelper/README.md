# CSharpEssentials.LoggerHelper

> **Route Serilog sinks by log level — zero boilerplate, native `ILogger<T>` support.**

Install the NuGet, add 5 lines of JSON, and every `ILogger<T>` in your app automatically routes logs to Console, File, Email, Telegram, PostgreSQL, SQL Server, Elasticsearch, and Seq — each sink receiving only the levels you want.

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
dotnet add package CSharpEssentials.LoggerHelper.Sink.Email   # add only what you need
```

---

## Why LoggerHelper?

| | Serilog alone | NLog | **LoggerHelper v5** |
|---|---|---|---|
| Per-level sink routing | Manual per sink | Via targets | **JSON / fluent — built-in** |
| `ILogger<T>` compatible | Via bridge pkg | Native | **Native — zero code change** |
| Install only needed sinks | No | No | **Yes — modular NuGet** |
| Named params preserved | Yes | Yes | **Yes — through the bridge** |
| `BeginScope` structured | Yes | Yes | **Yes — propagates to Serilog** |
| Scope nesting | Yes | Yes | **Yes — accumulates properties** |
| OpenTelemetry trace ID | Manual | Manual | **Built-in, auto-correlated** |
| Internal error diagnostics | No | No | **Yes — injectable ILogErrorStore** |
| Fluent OR JSON OR both | No | No | **All three, mergeable** |

---

## Quick Start — 30 seconds

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
      "File":  { "Path": "C:\\Logs\\MyApp", "RollingInterval": "Day" },
      "Email": { "To": "ops@example.com", "Host": "smtp.example.com", "Port": 587 }
    },
    "General": { "EnableRequestResponseLogging": true }
  }
}
```

That's it. Every `ILogger<T>` in your app now routes through LoggerHelper.

---

### Option B — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
    .AddRoute("File",    LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .AddRoute("Email",   LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureFile(f => { f.Path = "C:\\Logs\\MyApp"; f.RollingInterval = "Day"; })
    .EnableRequestResponseLogging()
);
```

### Option C — JSON base + fluent overrides (merge)

```csharp
// JSON defines shared config across environments.
// Fluent adds Development-only extras without touching JSON.
builder.Services.AddLoggerHelper(builder.Configuration, b => b
    .AddRoute("Console", LogEventLevel.Debug)  // extra sink only in code
);
```

### Option D — Via `ILoggingBuilder` (respects `Logging:LogLevel` filters)

```csharp
builder.Logging.ClearProviders();
builder.Logging.AddLoggerHelper(builder.Configuration);
```

---

## Zero code changes for existing apps

If your app already uses `ILogger<T>`, you change nothing. LoggerHelper plugs in as a standard `ILoggerProvider`:

```csharp
// Your existing service — not a single line changes
public class OrderService(ILogger<OrderService> logger) {
    public void Process(int orderId) {
        logger.LogInformation("Processing order {OrderId}", orderId);  // → Console + File
        logger.LogError("Payment failed for {OrderId}", orderId);      // → File + Email
    }
}
```

Named parameters like `{OrderId}` are preserved as structured Serilog properties — not flattened into strings.

---

## BeginScope — context that travels with your logs

Add business properties once. Every log in that block carries them automatically, including logs inside methods you call:

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> {
    ["OrderId"] = orderId,
    ["UserId"]  = userId
}))
{
    _logger.LogInformation("Validation started");   // OrderId + UserId are here
    await ValidateStock();                           // logs inside also carry them
    _logger.LogInformation("Order confirmed");       // OrderId + UserId are here
}
// Outside the using: properties removed automatically
```

**Scope nesting** — properties accumulate layer by layer:

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> { ["OrderId"] = 123 }))
{
    using (_logger.BeginScope(new Dictionary<string, object?> { ["Provider"] = "Stripe" }))
    {
        _logger.LogInformation("Charging card");
        // OrderId=123, Provider="Stripe"
    }
    _logger.LogInformation("Order complete");
    // OrderId=123  (Provider removed)
}
```

---

## Automatic enrichment on every log

Without any extra code, every log event carries:

| Property | Value |
|---|---|
| `ApplicationName` | Set via config or `WithApplicationName()` |
| `MachineName` | `Environment.MachineName` |
| `SourceContext` | Class name from `ILogger<T>` |
| `RenderedMessage` | Pre-rendered message string |
| `TraceId` / `SpanId` | From `System.Diagnostics.Activity` (OpenTelemetry) |
| `RequestPath` / `RequestId` | Added by ASP.NET Core middleware |

---

## Available sinks

Each sink is a separate NuGet — install only what you need:

| Package | Target |
|---|---|
| `CSharpEssentials.LoggerHelper.Sink.Console` | Colored console output |
| `CSharpEssentials.LoggerHelper.Sink.File` | JSON rolling files |
| `CSharpEssentials.LoggerHelper.Sink.Email` | SMTP alerts with throttling |
| `CSharpEssentials.LoggerHelper.Sink.Telegram` | Bot notifications with throttling |
| `CSharpEssentials.LoggerHelper.Sink.Postgresql` | PostgreSQL structured logs |
| `CSharpEssentials.LoggerHelper.Sink.MSSqlServer` | SQL Server structured logs |
| `CSharpEssentials.LoggerHelper.Sink.Elasticsearch` | Elasticsearch + Kibana |
| `CSharpEssentials.LoggerHelper.Sink.Seq` | Seq centralized log server |

Sinks self-register via a plugin mechanism — the core package has zero dependency on them.

---

## Per-level routing — real-world example

```json
"Routes": [
  { "Sink": "Console",       "Levels": ["Debug", "Information", "Warning"] },
  { "Sink": "File",          "Levels": ["Information", "Warning", "Error", "Fatal"] },
  { "Sink": "Telegram",      "Levels": ["Error", "Fatal"] },
  { "Sink": "Email",         "Levels": ["Fatal"] },
  { "Sink": "Elasticsearch", "Levels": ["Information", "Warning", "Error", "Fatal"] }
]
```

- Developers see everything on Console during development
- File captures all non-debug logs for auditing
- Telegram pings the team on every error
- Email sends a full report only for fatal crashes
- Elasticsearch indexes everything for Kibana dashboards

---

## Environment-aware configuration

LoggerHelper automatically picks the right file based on the current environment:

| Environment | File loaded |
|---|---|
| `Development` | `appsettings.LoggerHelper.debug.json` |
| Everything else | `appsettings.LoggerHelper.json` |

---

## Internal diagnostics

If a sink fails to configure (wrong connection string, unreachable SMTP server), LoggerHelper captures the error silently and exposes it — your app keeps running:

```csharp
app.MapGet("/health/logging", (ILogErrorStore errors) =>
    errors.Count == 0
        ? Results.Ok("All sinks healthy")
        : Results.Problem(string.Join("\n", errors.GetAll().Select(e => $"{e.SinkName}: {e.ErrorMessage}")))
);
```

---

## Request / Response logging middleware

Enable HTTP request/response logging with a single setting:

```json
"General": { "EnableRequestResponseLogging": true }
```

Then activate in the pipeline:
```csharp
app.UseLoggerHelper();
```

---

## Building a custom sink

1. Create a project `CSharpEssentials.LoggerHelper.Sink.MyTarget`
2. Implement `ISinkPlugin`:

```csharp
internal sealed class MyTargetSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        sinkName.Equals("MyTarget", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<MyTargetOptions>("MyTarget")
                   ?? options.BindSinkSection<MyTargetOptions>("MyTarget")
                   ?? new MyTargetOptions();

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

3. Reference `CSharpEssentials.LoggerHelper` as a NuGet — not a project reference.
4. The sink auto-registers at startup with no changes to the core.

---

## Target frameworks

| Package | Targets |
|---|---|
| LoggerHelper core | net8.0 · net9.0 · net10.0 |
| Sink plugins | net8.0 · net9.0 · net10.0 |

---

## TODO

> Planned for upcoming releases — contributions welcome.

- [ ] **Source Generator** — replace runtime reflection for sink loading with a compile-time source generator: faster startup, AOT-compatible, trimming-safe
- [ ] **BenchmarkDotNet suite** — published performance comparisons vs Serilog pure, NLog, and Microsoft.Extensions.Logging default provider
- [ ] **`dotnet new` template** — `dotnet new loggerhelper-api` scaffolds a pre-configured project with zero friction
- [ ] **Dashboard sink** — embedded real-time UI showing active sinks, routing rules, and recent sink errors
- [ ] **xUnit sink** — forwards log output to xUnit test runner for integration test visibility
- [ ] **AI extension** — natural language log queries, anomaly detection, and incident summarization via LLM
- [ ] **Telemetry extension** — OpenTelemetry metrics export (log counters per sink, error rates, latency)
- [ ] **Interactive playground** — browser-based editor to test JSON routing config and see live output

---

## License

MIT — © Alessandro Chiodo

[GitHub](https://github.com/alexbypa/CSharp.Essentials) · [NuGet](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
