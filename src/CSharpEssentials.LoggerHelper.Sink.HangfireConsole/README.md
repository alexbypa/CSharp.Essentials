# CSharpEssentials.LoggerHelper.Sink.HangfireConsole

> Route your `ILogger<T>` output directly to the Hangfire Dashboard console — with colors, levels, and zero manual Serilog wiring.

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.HangfireConsole.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.HangfireConsole)

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.HangfireConsole
dotnet add package Hangfire.Console
```

---

## Quick Start (3 steps)

### 1. Register the sink BEFORE LoggerHelper

> **Order matters.** `AddHangfireConsoleSink()` must be called **before** `AddLoggerHelper()` because the sink plugin reads the accessor during pipeline construction.

**Generic Host (Worker / BackgroundJobs)**

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) => {
        services.AddHangfireConsoleSink();           // 1st — register accessor
        services.AddLoggerHelper(ctx.Configuration); // 2nd — build Serilog pipeline

        services.AddHangfire(cfg => cfg
            .UseRedisStorage(connectionString)
            .UseConsole());                          // 3rd — enable Hangfire.Console
        services.AddHangfireServer();
    });
```

**WebApplicationBuilder (ASP.NET Core)**

```csharp
builder.Services.AddHangfireConsoleSink();
builder.Services.AddLoggerHelper(builder.Configuration);
```

### 2. Add the route in `appsettings.LoggerHelper.json`

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyWorker",
    "Routes": [
      { "Sink": "Console",         "Levels": ["Information","Warning","Error","Fatal"] },
      { "Sink": "HangfireConsole", "Levels": ["Information","Warning","Error","Fatal"] }
    ]
  }
}
```

No additional configuration needed in the `Sinks` section. The sink works out of the box with just the route declaration.

### 3. Set the PerformContext in your job

```csharp
public class MyJob {
    private readonly IPerformContextAccessor _ctx;
    private readonly ILogger<MyJob> _logger;

    public MyJob(IPerformContextAccessor ctx, ILogger<MyJob> logger) {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task Execute(PerformContext performContext) {
        _ctx.Set(performContext);
        try {
            _logger.LogInformation("This appears on the Dashboard!");
            _logger.LogWarning("This is yellow on the Dashboard");
        } finally {
            _ctx.Clear();
        }
    }
}
```

All `ILogger` calls between `Set()` and `Clear()` are routed to the Hangfire Dashboard with color-coded log levels.

---

## How It Works

```
ILogger.LogXxx()
  -> Serilog pipeline (built by LoggerHelper)
    -> HangfireConsoleSerilogSink.Emit()
      -> reads IPerformContextAccessor.Current (AsyncLocal, thread-safe)
      -> if inside a job  -> writes to Dashboard with color
      -> if outside a job -> skips silently
```

## Fluent API (alternative to JSON)

```csharp
services.AddHangfireConsoleSink();
services.AddLoggerHelper(b => b
    .WithApplicationName("MyWorker")
    .AddRoute("HangfireConsole",
        LogEventLevel.Information, LogEventLevel.Warning,
        LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureHangfireConsole(opts => {
        opts.FormatProvider = CultureInfo.InvariantCulture; // optional
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FormatProvider` | `IFormatProvider?` | `null` | Custom format provider for rendering log messages |

## Color Mapping

| Level | Color |
|-------|-------|
| Verbose | DarkGray |
| Debug | Gray |
| Information | White |
| Warning | Yellow |
| Error | Red |
| Fatal | DarkRed |

## Thread Safety

`IPerformContextAccessor` uses `AsyncLocal<T>` internally, so parallel jobs running on the same server never interfere with each other. Each async flow sees only its own `PerformContext`.

---

## Docker / Containerized Environments

Ensure `appsettings.LoggerHelper.json` is available inside the container:

**Option A — Volume mount (recommended for dev)**

```yaml
# docker-compose.yml
volumes:
  - ./MyWorker/appsettings.LoggerHelper.json:/app/appsettings.LoggerHelper.json
```

**Option B — Include in publish output (recommended for production)**

```xml
<Content Include="appsettings.LoggerHelper.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| ILogger on console but not on Dashboard | `AddHangfireConsoleSink()` called after `AddLoggerHelper()` | Swap the call order |
| Nothing anywhere | Config file missing or wrong format | Verify `LoggerHelper:Routes` exists (v5 format) |
| Only `RaiseMessage` on Dashboard | `Set(performContext)` not called | Inject `IPerformContextAccessor`, call `Set`/`Clear` |
| Works locally, not in Docker | Volume mounts wrong config file | Point to the per-project file, not a root-level copy |

---

## Requirements

| Dependency | Minimum Version |
|------------|----------------|
| .NET | 6.0+ |
| CSharpEssentials.LoggerHelper | 5.0.1+ |
| Hangfire.Console | 1.4.3+ |

## Changelog

### 5.0.2
- **Fix:** `AddHangfireConsoleSink()` now sets the internal static accessor — sink was silently inactive in 5.0.1.

### 5.0.1
- Initial release with LoggerHelper v5 plugin architecture.

---

## Links

- [Documentation](https://www.loggerhelper.it)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
