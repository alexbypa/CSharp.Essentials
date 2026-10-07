# CSharpEssentials.LoggerHelper.Sink.Console

> Colored console output with per-level color coding for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

**Targets:** `net8.0` · `net9.0` · `net10.0` — Part of the **CSharpEssentials.LoggerHelper** ecosystem. Install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
```

---

## Quick Setup — JSON

Add to `appsettings.json` (the `Routes` entry is all you need):

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Debug", "Information", "Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "Console": {
        "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}"
      }
    }
  }
}
```

Then register the helper and activate the pipeline:

```csharp
// Program.cs
using CSharpEssentials.LoggerHelper;

var builder = WebApplication.CreateBuilder(args);

// Register the LoggerHub using your configuration provider
builder.Services.AddLoggerHelper(builder.Configuration);

var app = builder.Build();
app.UseLoggerHelper();   // ← required: activates sinks and registers middleware
```

> **`Sinks.Console` is optional.** The sink works with just the route declaration.

---

## Quick Setup — Fluent API

```csharp
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Sink.Console;
using Serilog.Events;

builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Console", LogEventLevel.Debug, LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureConsole(c => c.OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}")
);

var app = builder.Build();
app.UseLoggerHelper();   // ← required
```

---

## What You'll See

Each line is printed in color according to the log level (local time, full level name):

```
[14:23:01 Information] Application started
[14:23:02 Warning] Retry attempt 1 for endpoint /api/orders
[14:23:03 Error] Unhandled exception: Connection refused
```

This is the default format (no `OutputTemplate`); when an exception is attached, its `ToString()` is appended to the same line. With a custom `OutputTemplate` (Serilog template syntax), include `{Exception}` explicitly to print exceptions.

---

## Configuration Options

| Property | Type | Default | Description |
|---|---|---|---|
| `OutputTemplate` | `string?` | `null` | Serilog output template (e.g. `[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}`). When null or empty, the default `[HH:mm:ss Level] Message` is used. Exceptions are printed only if `{Exception}` is in the template. |

---

## Color Mapping

| Level | Console Color |
|---|---|
| Verbose | DarkGray |
| Debug | Gray |
| Information | **Blue** |
| Warning | DarkYellow |
| Error | **Red** |
| Fatal | DarkRed |

Colors are applied per-line and reset automatically after each message.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No output at all | `app.UseLoggerHelper()` missing | Add it after `builder.Build()` |
| No colors in CI/Docker | Terminal does not support ANSI | Expected behavior in non-interactive terminals; output is still written |
| `Debug` lines missing | `Debug` not included in `Levels` | Add `"Debug"` to the `Routes` array for this sink |

---

## Links

- [Documentation](https://www.loggerhelper.it)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
