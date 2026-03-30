# 🐘 CSharpEssentials.LoggerHelper.Sink.PostgreSQL

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.PostgreSQL.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.PostgreSQL)
A flexible **PostgreSQL sink** for [CSharpEssentials.LoggerHelper](https://github.com/alexbypa/CSharp.Essentials), designed to store **structured logs** directly in PostgreSQL with support for **custom schemas, JSON fields, and automatic table creation**.

---
## Change Log ( 4.1.4 )
* **Connection resilience**: if PostgreSQL is unreachable at startup, the sink is skipped gracefully and the service starts normally instead of crashing.
* **Fast-fail timeout**: connection attempts during startup are capped at **3 seconds** to avoid blocking the ASP.NET Core pipeline.
* **Structured diagnostics**: connection errors are classified by category (`INFRASTRUCTURE`, `CREDENTIALS`, `NETWORK_TIMEOUT`, `CONFIGURATION`, `SSL`, `UNKNOWN`) with a human-readable `hint` field aimed at system administrators.
* **Minimal API endpoint**: a new `/api/sink-errors` endpoint exposes connection errors at runtime — see [Connection diagnostics endpoint](#connection-diagnostics-endpoint) below.

---

## 🔥 Key Features

* 🐘 Native PostgreSQL integration with **auto table creation**.
* 📊 Support for **custom schemas and column mappings**.
* 📦 Handles **JSON/JSONB fields** for structured data.
* ⚡ Perfect for **analytics, dashboards, and long-term log storage**.
* 🔧 Works seamlessly with LoggerHelper’s level-based sink routing.

---

## 📦 Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.PostgreSQL
```

---

## Connection diagnostics endpoint

When PostgreSQL is unreachable at startup the sink is automatically skipped, the service starts normally, and the error is stored in `GlobalLogger.Errors`. You can expose that information via a Minimal API endpoint directly in your client `Program.cs`.

### Why you need this

Without this endpoint, a connection failure is silent from the outside: the service runs but logs are never written to PostgreSQL. The endpoint lets you — or your monitoring system — detect the problem immediately without reading logs or restarting the service.

### How to add it in your client project

Add the `using` and the endpoint to your `Program.cs` as shown below. The endpoint must be registered **after** `app.Build()`.

```csharp
// Program.cs
using CSharpEssentials.LoggerHelper; // required for GlobalLogger

var builder = WebApplication.CreateBuilder(args);

// 1. Register LoggerHelper — must come before app.Build()
builder.Services.AddloggerConfiguration(builder.Configuration);

// ... your other services ...

var app = builder.Build();

// ... your other middleware ...

// 2. Register the diagnostics endpoint — must come after app.Build()
app.MapGet("/api/sink-errors", () =>
{
    var sinkErrors = GlobalLogger.Errors
        .Where(e => e.SinkName == "SelfLog")
        .OrderByDescending(e => e.Timestamp)
        .Select(e => new { e.Timestamp, e.SinkName, e.ErrorMessage })
        .ToList();

    return sinkErrors.Count == 0
        ? Results.Ok(new { status = "ok", message = "No sink errors detected.", errors = sinkErrors })
        : Results.Ok(new { status = "degraded", message = $"{sinkErrors.Count} sink error(s) detected.", errors = sinkErrors });
});

app.Run();
```

> `AddloggerConfiguration` must always be called before `app.Build()`.
> `MapGet("/api/sink-errors", ...)` must always be called after `app.Build()`.

### Response when everything is fine

```json
{
  "status": "ok",
  "message": "No sink errors detected.",
  "errors": []
}
```

### Response when PostgreSQL is unreachable

```json
{
  "status": "degraded",
  "message": "1 sink error(s) detected.",
  "errors": [
    {
      "timestamp": "2026-03-30T09:39:55Z",
      "sinkName": "SelfLog",
      "errorMessage": "[PostgreSQL sink skipped] CATEGORY=INFRASTRUCTURE | TARGET=Host=myhost;Port=5432;Database=mydb;Username=myuser | [NpgsqlException] Exception while reading from stream | InnerException: [IOException] Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host. | HINT=The PostgreSQL server is unreachable. Check that the server is running, the host/port are correct, and that no firewall is blocking port 5432. This is NOT an application bug."
    }
  ]
}
```

### Error categories

The `CATEGORY` and `HINT` fields inside `errorMessage` are intended for **system administrators**, not developers. Each category maps to a specific infrastructure action:

| Category | Root cause | What the sysadmin should check |
|---|---|---|
| `INFRASTRUCTURE` | Server down, firewall, wrong host/port | PostgreSQL service status, network route, port 5432 open |
| `CREDENTIALS` | Wrong username/password, `pg_hba.conf` | Connection string credentials, server auth config |
| `NETWORK_TIMEOUT` | Server overloaded, firewall dropping packets | Server load, firewall rules (silent drop vs reject) |
| `CONFIGURATION` | Database does not exist | Database name in the connection string |
| `SSL` | TLS/certificate mismatch | SSL mode in connection string, server certificate |
| `UNKNOWN` | Unexpected error | Escalate full `errorMessage` to the development team |

> All categories except `UNKNOWN` indicate an **infrastructure problem, not an application bug**.

---

## Demo Project

A full working demo with PostgreSQL integration is available here:
[**CSharpEssentials.Extensions Demo**](https://github.com/alexbypa/Csharp.Essentials.Extensions/tree/main)

---