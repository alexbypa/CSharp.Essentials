# CSharpEssentials.LoggerHelper.Dashboard

> **Embedded real-time diagnostics UI for LoggerHelper — zero external dependencies.**

Add one line to your ASP.NET Core app and get a live dashboard showing sink health, startup errors, routing configuration, and system status. No Seq, no Kibana, no Grafana required.

## Quick Start

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Dashboard
dotnet add package CSharpEssentials.LoggerHelper.Sink.Console  # add sinks you need
```

```csharp
// Program.cs
builder.Services.AddLoggerHelper(builder.Configuration);

var app = builder.Build();

app.UseLoggerHelper();
app.MapLoggerHelperDashboard();  // serves at /loggerhelper-dashboard

app.Run();
```

Open `https://localhost:5001/loggerhelper-dashboard` in your browser.

## What You See

| Section | Details |
|---------|---------|
| **Status cards** | Overall health (OK / WARNING / CRITICAL), active sinks, failed sinks, error count |
| **Sink Errors** | Every error that prevented a sink from starting — with timestamp, message, stack trace, and context. Click a row to expand details |
| **Sinks** | All configured sinks with ACTIVE/FAILED status, plugin type, and assigned log levels |
| **Routing** | Complete routing configuration: which levels go to which sinks |

## Features

- **Zero dependencies** — self-contained HTML served inline, no npm, no static files
- **Auto-refresh** — updates every 30 seconds, or click Refresh manually
- **Dark theme** — GitHub-inspired dark UI, responsive on mobile
- **Startup error visibility** — see exactly WHY a sink failed to initialize (wrong connection string, unreachable host, missing config)
- **Click-to-expand** — error rows expand to show full stack trace and context

## Custom Path

```csharp
app.MapLoggerHelperDashboard("/admin/logging");
```

## Why This Matters

Serilog has no built-in dashboard. To see sink health you need Seq ($), Kibana (complex), or Grafana (infrastructure). LoggerHelper gives you this for free — one line of code, zero infrastructure.

---

MIT — © Alessandro Chiodo

[Documentation](https://www.loggerhelper.com) · [GitHub](https://github.com/alexbypa/CSharp.Essentials) · [NuGet](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
