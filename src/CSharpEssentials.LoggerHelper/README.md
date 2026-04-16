# CSharpEssentials.LoggerHelper

**The easiest way to orchestrate Serilog sinks.**

Route structured logs by level to Console, File, Email, Telegram, Elasticsearch, PostgreSQL, SQL Server, Seq and more — with a simple JSON config or fluent C# API.

## Quick Start (5 lines)

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error)
    .AddRoute("Email", LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureEmail(e => { e.To = "ops@example.com"; e.Host = "smtp.example.com"; })
);
```

That's it. Every `ILogger<T>` in your app now routes through LoggerHelper.

## JSON Configuration

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning", "Error"] },
      { "Sink": "Email",   "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "Email": {
        "To": "ops@example.com",
        "Host": "smtp.example.com",
        "Port": 587
      }
    }
  }
}
```

## Features

- Per-level sink routing via JSON or fluent API
- Native `ILogger<T>` support (Microsoft.Extensions.Logging)
- Modular sinks (install only what you need)
- OpenTelemetry trace correlation built-in
- Request/response HTTP logging middleware
- Sink throttling for Email/Telegram
- Internal error diagnostics

## Install

```
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console
dotnet add package CSharpEssentials.LoggerHelper.Sink.Email
```

## Documentation

https://github.com/alexbypa/CSharp.Essentials
