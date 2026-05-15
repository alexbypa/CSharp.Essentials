# Route Serilog Sinks by Level in 5 Lines of JSON

**Published on:** dev.to (draft for launch)

LoggerHelper v5 is a Serilog **orchestrator**, not a replacement. If you already use `ILogger<T>`, you keep your code — only configuration changes.

## Quick start

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.File
```

```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
app.UseLoggerHelper();
```

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApi",
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning"] },
      { "Sink": "File", "Levels": ["Error", "Fatal"] }
    ]
  }
}
```

## Why v5?

- Native `ILogger<T>` — zero application code changes
- JSON-first routing — errors to email, info to console
- Modular sinks — install only what you need
- Source-generated plugin registration — faster startup, trimming-safe

## Template

```bash
dotnet new install ./templates
dotnet new loggerhelper-api -n MyApi
```

## Links

- GitHub: https://github.com/alexbypa/CSharp.Essentials
- Benchmarks: see `docs/benchmarks.md`
