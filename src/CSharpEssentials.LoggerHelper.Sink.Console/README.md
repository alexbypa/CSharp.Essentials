# CSharpEssentials.LoggerHelper.Sink.Console

> Colored console output with per-level color coding for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
```

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning", "Error"] }
    ]
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
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error)
    .ConfigureConsole(c => c.OutputTemplate = "[{Timestamp:HH:mm:ss} {Level}] {Message}")
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `OutputTemplate` | `string?` | `null` | Custom output template for log formatting |

---

## Color Mapping

| Level | Color |
|-------|-------|
| Verbose | DarkGray |
| Debug | Gray |
| Information | Blue |
| Warning | DarkYellow |
| Error | Red |
| Fatal | DarkRed |

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
