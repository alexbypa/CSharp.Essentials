# LoggerHelper v5 — Parity vs Legacy README

Reference: [Csharp.Essentials.Extensions README](https://github.com/alexbypa/Csharp.Essentials.Extensions/blob/main/README.md)

## Summary: v5 is equal or better

| Area | Legacy | v5 | Status |
|---|---|---|---|
| Per-level JSON routing | SerilogCondition | LoggerHelper:Routes | Better (simpler schema) |
| Legacy JSON without migration | Serilog:SerilogConfiguration | Auto-detected adapter | Better |
| ILogger&lt;T&gt; | loggerExtension only | Native MEL provider | Better |
| BeginScope | Broken (null) | Full Serilog LogContext | Better |
| IContextLogEnricher | Yes | Yes (DI) | Equal |
| IRequest / IdTransaction | loggerExtension.TraceSync | LogWithRequest + BeginTrace | Better |
| ILogErrorStore | Static Errors | ILogErrorStore injectable | Better |
| Loaded sinks diagnostics | LoadedSinkInfo static | ILoadedSinkStore | Better |
| Email HTML + throttle | Yes | Yes | Equal |
| Telegram throttle | Yes | Yes + legacy keys | Equal |
| PostgreSQL custom columns | ColumnsPostGreSQL | Columns / ColumnsPostGreSQL | Equal |
| MSSQL additional columns | Yes | Yes + nested legacy JSON | Equal |
| Elasticsearch | nodeUris | NodeUris + alias | Equal |
| Seq ApiKey | No | Yes | Better |
| Source generator | Reflection only | SG + filesystem fallback | Better |
| Dashboard sink | Separate package | Planned (out of v5 core) | Deferred |
| xUnit sink | Separate package | Planned | Deferred |
| Telemetry / AI | Separate packages | Planned | Deferred |

## Migration

Use new JSON under `LoggerHelper`, or keep existing `Serilog:SerilogConfiguration` — v5 loads it automatically.

## Recommended v5 setup

```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
builder.Services.AddSingleton<IContextLogEnricher, MyCustomEnricher>(); // optional
app.UseLoggerHelper();
```

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Console", "Levels": ["Information", "Warning"] },
      { "Sink": "Email", "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "Email": {
        "Host": "smtp.example.com",
        "Port": 587,
        "From": "noreply@example.com",
        "To": "ops@example.com",
        "ThrottleInterval": "00:01:00"
      }
    }
  }
}
```
