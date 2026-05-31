# CSharpEssentials.LoggerHelper.Sink.HangfireConsole

> Write structured logs to the Hangfire Dashboard console during job execution for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.HangfireConsole
```

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "HangfireConsole", "Levels": ["Information", "Warning", "Error"] }
    ]
  }
}
```

```csharp
// 1. Register the PerformContext accessor in DI
builder.Services.AddHangfireConsoleSink();

// 2. Add LoggerHelper with JSON config
builder.Services.AddLoggerHelper(builder.Configuration);
```

## Quick Setup — Fluent API

```csharp
builder.Services.AddHangfireConsoleSink();

builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("HangfireConsole", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error)
);
```

---

## Usage in Hangfire Jobs

The sink requires setting the `PerformContext` at the start of each job:

```csharp
public class ProcessOrderJob(
    ILogger<ProcessOrderJob> logger,
    IPerformContextAccessor contextAccessor)
{
    public void Execute(PerformContext context, int orderId)
    {
        contextAccessor.Set(context);    // Activate dashboard logging

        logger.LogInformation("Processing order {OrderId}", orderId);
        // ... your job logic ...
        logger.LogInformation("Order {OrderId} completed", orderId);

        contextAccessor.Clear();         // Cleanup
    }
}
```

All `ILogger` calls within the job will appear on the Hangfire Dashboard with colored output. Outside of a Hangfire job, logs are silently ignored.

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

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
