# CSharpEssentials.LoggerHelper.Sink.File

> Rolling JSON log files with configurable retention for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
```

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "File", "Levels": ["Information", "Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "File": {
        "Path": "C:\\Logs\\MyApp",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 14
      }
    }
  }
}
```

```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
```

## Quick Setup — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("File", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureFile(f => {
        f.Path = "C:\\Logs\\MyApp";
        f.RollingInterval = "Day";
        f.RetainedFileCountLimit = 14;
    })
);
```

---

## Dynamic File Routing by Property (v5.1.0)

Route log files into **subdirectories** based on a log event property value. Perfect for multi-tenant apps, per-module separation, or any scenario where you need logs organized by a runtime value.

### JSON config

```json
"Sinks": {
  "File": {
    "Path": "Logs",
    "RollingInterval": "Day",
    "FileNameProperty": "TenantId"
  }
}
```

### How it works

```csharp
// Logs with TenantId → Logs/acme/log-20260531.txt
using (_logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = "acme" }))
{
    _logger.LogInformation("Order processed");   // → Logs/acme/log-.txt
    _logger.LogError("Payment failed");           // → Logs/acme/log-.txt
}

// Logs without TenantId → Logs/log-20260531.txt (base path)
_logger.LogInformation("App started");            // → Logs/log-.txt
```

### Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .AddRoute("File", LogEventLevel.Information, LogEventLevel.Error)
    .ConfigureFile(f => {
        f.Path = "Logs";
        f.FileNameProperty = "TenantId";
        f.MaxOpenFiles = 128;   // LRU pool limit (default: 64)
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Path` | `string` | `"Logs"` | Directory path for log files |
| `RollingInterval` | `string` | `"Day"` | Rolling interval: `Minute`, `Hour`, `Day`, `Month`, `Year`, `Infinite` |
| `RetainedFileCountLimit` | `int` | `7` | Number of log files to retain before cleanup |
| `Shared` | `bool` | `true` | Allow multiple processes to write to the same file |
| `FileNameProperty` | `string?` | `null` | Log event property used to create subdirectories (e.g. `"TenantId"`) |
| `MaxOpenFiles` | `int` | `64` | Max open file handles when using `FileNameProperty` (LRU eviction) |

Logs are written in **JSON format** using Serilog's `JsonFormatter` for structured log analysis.

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
