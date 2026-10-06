# CSharpEssentials.LoggerHelper.Dashboard

Embedded real-time diagnostics dashboard for ASP.NET Core — no Seq, no Kibana, no extra infrastructure.

Navigate to `/loggerhelper` and see the health of your logging pipeline at a glance: active sinks, live log stream, error history, and the full context that preceded your last crash.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper.Dashboard
```
Requires: `CSharpEssentials.LoggerHelper` ≥ 5.2.0 · **Targets:** net8.0 · net9.0 · net10.0
Requires: `CSharpEssentials.LoggerHelper` ≥ 5.2.0

---

## Wire up (Program.cs)

```csharp
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoggerHelper(builder.Configuration);
builder.Services.AddLoggerHelperDashboard(o =>
    o.UseBasicAuthentication("admin", builder.Configuration["Dashboard:Password"]));

var app = builder.Build();
app.UseLoggerHelper();
app.MapLoggerHelperDashboard();   // → /loggerhelper

app.Run();
```

Navigate to `https://localhost:5001/loggerhelper` (the browser asks for the credentials). Authentication is mandatory, see [Protect with authentication](#protect-with-authentication).

---

## Features

### Sink Health Cards
At-a-glance status for every configured sink: **ACTIVE** / **FAILED** badges, assigned log levels, toggle controls. The overall health indicator (OK / WARNING / CRITICAL) updates automatically on each refresh. A sink whose configuration is missing or incomplete (e.g. empty `ConnectionString`) is shown as **FAILED**.

### Live Log Stream
Browser-based `tail -f` via Server-Sent Events. Filter by level or free-text in real time. Toggle the Live switch to pause without disconnecting.

### Error History
Click-to-expand table of recent sink errors (SMTP failures, DB write errors, network timeouts) with full stack traces. Never lose a sink error silently.

### Context Before Error *(requires contextual logging)*
The most powerful panel: when an Error or Fatal event fires, the zero-allocation ring buffer automatically flushes all preceding Debug/Info/Warning entries to the Dashboard. You see **exactly what happened before the crash** — without keeping verbose logging on permanently.

```
09:55:07.136  [Information]  HashCollectorWorker started. Interval: 60min
09:55:07.208  [Information]  Now listening on: http://localhost:5055
09:55:07.452  [Warning]      Slow query: 2340ms on GetOrders
─────────────────── ▼ Triggering event ──────────────────────
09:55:08.301  [Error]        NullReferenceException in OrderService.Process
```

Enable in `appsettings.json`:
```json
{
  "LoggerHelper": {
    "General": {
      "EnableContextualLogging": true,
      "ContextualBufferCapacity": 200
    }
  }
}
```

> **How it works:** a lock-free ring buffer (`Interlocked`, zero heap allocations after startup) retains the last N log entries. On Error/Fatal the buffer flushes context to your configured sinks with `IsContextualHistory = true`, and the Dashboard reads the flushed entries via `/api/status`. The triggering Error/Fatal is stored separately and shown with a red "▼ Triggering event" separator — it never enters the ring buffer, preventing feedback loops.

### Routing Configuration
Visual table of which log levels go to which sinks — useful when diagnosing why a specific message did or did not appear in a given sink.

### Runtime Controls
Toggle any sink on/off or change log levels without restarting the application. Works in conjunction with the MCP server (`CSharpEssentials.LoggerHelper.MCP`) for AI-driven control.

---

## Configuration options

```csharp
builder.Services.AddLoggerHelperDashboard(options => {
    options.Path = "/loggerhelper";         // default, change to any route
    options.RefreshIntervalSeconds = 15;    // default 30s
    options.AuthorizationPolicy = "Admins"; // optional named policy
    options.UseBasicAuthentication("admin", password); // optional built-in Basic auth
});
```

| Option | Default | Description |
|---|---|---|
| `Path` | `/loggerhelper` | Base route of the dashboard and its API |
| `RefreshIntervalSeconds` | `30` | Auto-refresh interval |
| `AuthorizationPolicy` | none | Name of an ASP.NET Core authorization policy required on all routes |
| `UseBasicAuthentication(username, password)` | none | Built-in HTTP Basic authentication. No default credentials; empty username or password throws `ArgumentException` |

### Protect with authentication

Authentication is **mandatory** on every route (HTML, `/api/status`, `/api/logs`, `/api/stream`). There is no anonymous mode. Pick one of three modes:

**1. Your app's authentication** (default scheme):

```csharp
builder.Services.AddAuthentication(/* your scheme */).AddJwtBearer(); // needs Microsoft.AspNetCore.Authentication.JwtBearer
builder.Services.AddAuthorization();
builder.Services.AddLoggerHelperDashboard();

app.UseAuthentication();
app.UseAuthorization();
app.MapLoggerHelperDashboard();
```

**2. A named authorization policy:**

```csharp
builder.Services.AddAuthorization(o => o.AddPolicy("Admins", p => p.RequireRole("Admin")));
builder.Services.AddLoggerHelperDashboard(o => o.AuthorizationPolicy = "Admins");
```

**3. Built-in Basic authentication** (apps without their own auth):

```csharp
builder.Services.AddLoggerHelperDashboard(o =>
    o.UseBasicAuthentication("admin", builder.Configuration["Dashboard:Password"]));
```

Basic + `AuthorizationPolicy` together = **AND**: the request must carry valid Basic credentials and satisfy the policy.

Notes on Basic authentication:

- **Use it over HTTPS only.** Credentials travel base64-encoded, not encrypted.
- **Keep the password out of `appsettings.json`.** Development: `dotnet user-secrets set "Dashboard:Password" "..."`. Production: an environment variable (`Dashboard__Password`) or a secret store (Azure Key Vault, AWS Secrets Manager...).
- Comparison is constant-time; failures return `401` with `WWW-Authenticate` (the browser prompt also covers the SSE stream).
- There is **no brute-force protection**: run it behind HTTPS and your app's rate limiter.
- Side effect: if Basic is the only authentication scheme registered, it becomes the default scheme (.NET 7+ auto default; disable with the `Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme` AppContext switch).

---

## JSON API endpoints

The Dashboard exposes four routes (all require authentication) you can call directly (useful for integration tests or external monitoring):

| Endpoint | Description |
|---|---|
| `GET /loggerhelper` | The HTML dashboard |
| `GET /loggerhelper/api/status` | JSON: health, sinks, errors, lastFlush with context entries |
| `GET /loggerhelper/api/logs` | JSON: ring buffer snapshot with optional `?level=` and `?query=` filters |
| `GET /loggerhelper/api/stream` | SSE stream: live log entries as `data: {...}` events |

```bash
# Check health from CI or monitoring
curl -u admin:yourpassword https://myapp.com/loggerhelper/api/status
```

---

## Troubleshooting

### `InvalidOperationException` at startup from `MapLoggerHelperDashboard`
No authentication is configured, `AddLoggerHelperDashboard` was not called, or `AuthorizationPolicy` names an unknown policy. Call `AddLoggerHelperDashboard`, then either `UseBasicAuthentication(...)`, register your authentication, or add the policy.

### 401 on every request
Credentials missing or wrong (Basic), or the app's authentication did not authenticate the user. API calls need `curl -u user:pass`.

### Dashboard shows "LOADING..." permanently
The JavaScript derives its base path from `window.location.pathname` at runtime. If you serve the app behind a reverse proxy with a path prefix, make sure the prefix is included in the URL you navigate to — the dashboard detects it automatically.

### Context Before Error panel never appears
- Confirm `EnableContextualLogging: true` is set in your config
- The panel only shows after at least one Error or Fatal has been logged — trigger one via your demo endpoint or a test request
- If contextual logging is disabled, the panel is hidden

### Context Before Error shows only the triggering event, no preceding entries
Verify you are on version **≥ 5.2.2**. Earlier builds had a feedback loop where re-emitted context entries (with `IsContextualHistory = true`) looped back through the pipeline as new Error events, overwriting `_lastFlush` and clearing the context entries.

### SSE stream disconnects on client close
Normal behavior — `OperationCanceledException` on disconnect is caught internally. The browser reconnects automatically within 5 seconds.

---

## Zero dependencies

Pure HTML/CSS/JS served as an embedded string resource. No npm, no bundler, no CDN calls at runtime. The package maps one route group (4 routes) into your ASP.NET Core app.

---

## License

MIT — [loggerhelper.it](https://www.loggerhelper.it) · [GitHub](https://github.com/alexbypa/CSharp.Essentials)