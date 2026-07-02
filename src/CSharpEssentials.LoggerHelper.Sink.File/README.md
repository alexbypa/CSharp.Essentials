# CSharpEssentials.LoggerHelper.Sink.File

> Rolling JSON log files with configurable retention and dynamic per-property routing for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

**Targets:** `net8.0` · `net9.0` · `net10.0` — Part of the **CSharpEssentials.LoggerHelper** ecosystem. Install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
```

---

## Quick Setup — JSON

Add to `appsettings.json`:

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "File", "Levels": ["Information", "Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "File": {
        "Path": "Logs",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 14
      }
    }
  }
}
```

```csharp
// Program.cs
builder.Services.AddLoggerHelper(builder.Configuration);

var app = builder.Build();
app.UseLoggerHelper();   // ← required: activates sinks and registers middleware
```

> **Absolute paths** work on both Windows and Linux:
> - Windows: `"C:\\Logs\\MyApp"` or `"C:/Logs/MyApp"`
> - Linux / Docker: `"/var/log/myapp"` or `"logs"` (relative to the working directory)

---

## Quick Setup — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("File", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureFile(f => {
        f.Path                  = "Logs";
        f.RollingInterval       = "Day";
        f.RetainedFileCountLimit = 14;
    })
);

var app = builder.Build();
app.UseLoggerHelper();   // ← required
```

---

## What You'll See

Each log event is written as a **single JSON line**:

```json
{"@t":"2026-06-01T14:23:01.1230000Z","@mt":"Order {OrderId} placed by {UserId}","@l":"Information","OrderId":42,"UserId":"usr_99","ApplicationName":"MyApp","SourceContext":"OrdersController"}
```

Fields at a glance:

| Field | Description |
|---|---|
| `@t` | UTC timestamp (ISO 8601) |
| `@mt` | Raw message template |
| `@l` | Log level (omitted when `Information`) |
| `@x` | Exception string (present on errors) |
| Any extra key | Structured property pushed via scope or call-site |

Files are named `log-YYYYMMDD.txt` by default and roll at midnight.

---

## Dynamic File Routing by Property

Route logs into **separate subdirectories** based on any runtime property — ideal for multi-tenant apps, per-module separation, or environment-based routing.

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
// Logs with TenantId="acme" → Logs/acme/log-20260601.txt
using (_logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = "acme" }))
{
    _logger.LogInformation("Order processed");
    _logger.LogError("Payment failed");
}

// Logs without TenantId → Logs/log-20260601.txt  (base path, no subdirectory)
_logger.LogInformation("App started");
```

### Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .AddRoute("File", LogEventLevel.Information, LogEventLevel.Error)
    .ConfigureFile(f => {
        f.Path             = "Logs";
        f.FileNameProperty = "TenantId";
        f.MaxOpenFiles     = 128;   // LRU pool limit for open file handles (default: 64)
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Path` | `string` | `"Logs"` | Base directory for log files. Relative paths are resolved from the app working directory. |
| `RollingInterval` | `string` | `"Day"` | When to start a new file: `Minute`, `Hour`, `Day`, `Month`, `Year`, `Infinite` (single file, never rolls). |
| `RetainedFileCountLimit` | `int` | `7` | How many rolled files to keep before the oldest is deleted. |
| `Shared` | `bool` | `true` | Allow multiple processes (e.g. multiple app instances) to write to the same file. |
| `FileNameProperty` | `string?` | `null` | Log event property used to create per-value subdirectories (e.g. `"TenantId"`). See section above. |
| `MaxOpenFiles` | `int` | `64` | Maximum number of simultaneously open file handles when using `FileNameProperty`. Oldest handles are closed on LRU eviction. |

> All logs are written in **structured JSON format** using Serilog's `JsonFormatter`. There is no plain-text mode for this sink — use the Console sink for human-readable output.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No file created | `app.UseLoggerHelper()` missing, or write permission denied | Add `app.UseLoggerHelper()` and verify the process has write access to `Path` |
| All logs in base folder (no subdirectory) | `FileNameProperty` not set in scope before logging | Wrap log calls with `BeginScope` containing the property |
| `Too many open files` OS error | `MaxOpenFiles` too high for the OS limit | Reduce `MaxOpenFiles` or raise the OS `ulimit -n` |
| Old files not deleted | `RetainedFileCountLimit` reached but files are locked | Check for other processes holding file handles |
| Logs from different log levels mixed | All levels route to the same file | Use separate `File` sink instances with different `Path` values and different `Routes` |

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
