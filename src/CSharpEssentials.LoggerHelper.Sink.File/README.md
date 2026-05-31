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

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Path` | `string` | `"Logs"` | Directory path for log files |
| `RollingInterval` | `string` | `"Day"` | Rolling interval: `Minute`, `Hour`, `Day`, `Month`, `Year`, `Infinite` |
| `RetainedFileCountLimit` | `int` | `7` | Number of log files to retain before cleanup |
| `Shared` | `bool` | `true` | Allow multiple processes to write to the same file |

Logs are written in **JSON format** using Serilog's `JsonFormatter` for structured log analysis.

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
