# CSharpEssentials.LoggerHelper.Sink.Seq

> Seq centralized structured log server integration for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

**Targets:** `net8.0` · `net9.0` · `net10.0` — Part of the **CSharpEssentials.LoggerHelper** ecosystem. Install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Seq
```

---

## Quick Setup — JSON

Add to `appsettings.json`:

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
// Program.cs
builder.Services.AddLoggerHelper(builder.Configuration);

var app = builder.Build();
app.UseLoggerHelper();   // ← required: activates sinks and registers middleware
```

> **`ApiKey` is optional for local development.** A local Seq instance (single-user, no auth) works with `ApiKey` omitted entirely.

---

## Quick Setup — Fluent API

```csharp
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Sink.Seq;
using Serilog.Events;

builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Seq", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureSeq(s => {
        s.ServerUrl = "http://localhost:5341";
        s.ApiKey    = "your-api-key";   // omit for local no-auth Seq
    })
);

var app = builder.Build();
app.UseLoggerHelper();   // ← required
```

---

## What You'll See

Log events appear in the **Seq web UI** at `http://localhost:5341` with full structured property support — filter, search, and alert on any property value:

```
2026-06-01 14:23:01 [INF] Order 42 placed by usr_99
  OrderId    = 42
  UserId     = "usr_99"
  TenantId   = "acme"
  ApplicationName = "MyApp"
```

All properties set via `BeginScope` or call-site parameters are indexed and searchable in Seq's SQL-like query language:

```sql
-- Seq filter examples
ApplicationName = 'MyApp' and @Level = 'Error'
TenantId = 'acme' and OrderId > 100
```

---

## Configuration Options

| Property | Type | Default | Description |
|---|---|---|---|
| `ServerUrl` | `string` | `""` | **Required.** Seq ingestion URL (default port is `5341`). Legacy JSON key `serverUrl` is also accepted. |
| `ApiKey` | `string?` | `null` | API key for authentication. Optional for local single-user Seq instances. Required for production Seq Server or Seq Cloud. |

> An empty `ServerUrl` (or a missing `Seq` section) makes the sink fail to configure with an `InvalidOperationException`; LoggerHelper records it as not configured and the other sinks keep working. The v4 section name `SeqOptions` is read as a fallback.

---

## Quick Local Setup with Docker

The fastest way to run Seq locally:

```bash
docker run -d --name seq \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  datalust/seq
```

Open the Seq UI at `http://localhost:5341`. No API key needed for local single-user mode.

For **Seq Cloud** or a production server, generate an API key in *Settings → API Keys* and set it in `ApiKey`.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No output at all | `app.UseLoggerHelper()` missing | Add it after `builder.Build()` |
| Sink shows FAILED in Dashboard/MCP | `ServerUrl` empty or `Seq` section missing: the sink throws `InvalidOperationException` at configure time and is recorded as not configured | Set `Sinks.Seq.ServerUrl` |
| No events appear in Seq UI | `ServerUrl` not reachable | Check the URL and ensure Seq is running; `curl http://localhost:5341/api` should return JSON |
| `401 Unauthorized` in logs | Wrong or missing `ApiKey` | Verify the key in Seq *Settings → API Keys* |
| Nothing arrives and no error is shown | Sink errors are silent by default | Enable `General.EnableSelfLogging` (JSON `"General": { "EnableSelfLogging": true }` or `.EnableSelfLogging()` on the builder) to see Serilog `SelfLog` diagnostics |
| Events delayed | Network latency or Seq ingestion backpressure | Check Seq server health; reduce the number of events per second if needed |

---

## Links

- [Seq — Structured log server](https://datalust.co/seq)
- [Seq Documentation](https://docs.datalust.co)
- [Documentation](https://www.loggerhelper.it)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
