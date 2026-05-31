# CSharpEssentials.LoggerHelper.Sink.Seq

> Seq centralized log server integration for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Seq
```

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Seq", "Levels": ["Information", "Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "Seq": {
        "ServerUrl": "http://localhost:5341",
        "ApiKey": "your-api-key"
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
    .AddRoute("Seq", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureSeq(s => {
        s.ServerUrl = "http://localhost:5341";
        s.ApiKey = "your-api-key";
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServerUrl` | `string` | `""` | Seq server URL |
| `ApiKey` | `string?` | `null` | API key for authentication (optional) |

---

## Links

- [Seq — Structured log server](https://datalust.co/seq)
- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
