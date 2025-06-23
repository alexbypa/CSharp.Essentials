# CSharpEssentials.LoggerHelper.Sink.Telemetry

## 📑 Table of Contents <a id='table-of-contents'></a>
* 🚀[Installation](#installation)
* 🔧[Configuration](#configuration)
* 📊 What It Does(#whatitdoes)
* 📊 Custom Metrics(#custommetrics)
* 🧵 Traces (activities, spans)
* 📘 Logs (linked by `trace_id`)

---

Plug-and-play extension that integrates **Serilog** and **OpenTelemetry** with direct **PostgreSQL export** of:

## 📦 Installation<a id='installation'></a>   [🔝](#table-of-contents)
```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Telemetry
```

---

## ⚙️ Configuration<a id='configuration'></a>   [🔝](#table-of-contents)

In your `Program.cs` (or `Startup.cs` for older .NET versions), register the telemetry system:

```csharp
builder.Services.AddLoggerTelemetry(builder);
```

Then add the following configuration file `appsettings.LoggerHelper.json`:

```json
"Serilog": {
  "SerilogConfiguration": {
    "LoggerTelemetryOptions": {
      "IsEnabled": true,
      "ConnectionString": "Host=localhost;Database=metrics_db;Username=user;Password=pass"
    }
  }
}
```

---

## 🚀 What It Does<a id='whatitdoes'></a>   [🔝](#table-of-contents)

### 🔧 LoggerTelemetryBuilder

This method wires up:

- `TelemetriesDbContext` (EF Core)
- `OpenTelemetryMeterListenerService`
- ASP.NET + HttpClient instrumentation
- Runtime + custom metrics
- Exporters: PostgreSQL and console

### 📡 PostgreSqlMetricExporter

Stores each OpenTelemetry metric as a `MetricEntry` in PostgreSQL, capturing:

- `Name`
- `Value`
- `Timestamp`
- `TraceId`
- `TagsJson` (all tags serialized)

### 🧵 PostgreSqlTraceExporter

Captures every `Activity` and saves it as a `TraceEntry`.

---

## ✨ Custom Metrics<a id='custommetrics'></a>   [🔝](#table-of-contents)

The package includes:

- `GaugeWrapper` → create observable gauges easily
- Predefined metrics:
  - `memory_used_mb`
  - `postgresql.connections.active`
  - ...and extendable via `CustomMetrics`

---

## 🌐 Public API Controller

A built-in controller `TelemetryPublicApi.cs` exposes:

- `GET /api/TelemetryPublicApi/metrics`
- Easily extendable to `/traces`, `/errors`, `/health`, etc.

---

## 🧪 Minimal Example

```csharp
var meter = new Meter("CustomApp");
meter.CreateObservableGauge("app.threads.count", () => ThreadPool.ThreadCount);

app.MapGet("/", () => "Hello LoggerHelper!");
```

---

## 🧠 Internals Explained – Trace Correlation Middleware

### 🔗 TraceIdPropagationMiddleware

This middleware is the **core of trace correlation** within the `LoggerHelper.Telemetry` package.  
It ensures every incoming HTTP request has its `TraceId` consistently injected into:

- **Traces** (`Activity`) via `SetTag("trace_id", ...)`
- **Metrics** via OpenTelemetry `Baggage.SetBaggage(...)`
- **Logs** (if using the `ILogTraceContext<T>` implementation)

```csharp
var traceId = Activity.Current?.TraceId.ToString();
activity.SetTag("trace_id", traceId);
Baggage.SetBaggage("trace_id", traceId);
```

### ✅ Why it matters

- Makes **traces** easily searchable by `trace_id`
- Allows **metrics** to be filtered or grouped by `trace_id` (e.g. `request duration`, `memory usage`)
- Enables **logs** to be enriched with trace context, allowing full end-to-end observability

### 📈 Result

> All telemetry signals – logs, metrics, traces – share a common `trace_id`.  
This makes it easy to:
- Debug distributed flows
- Visualize latency and performance breakdowns
- Cross-navigate from a log to its metric to its trace

---

## 📈 Dashboard (Roadmap)

Coming soon:

- 📊 React dashboard (HangFire-style)
- ⏰ Alert system via BackgroundService
- 🔍 Query by `trace_id`, `name`, `tag`, and more

---

## 📁 Project Structure

```
EF/
├─ Data/
│  └─ TelemetriesDbContext.cs
├─ Models/
│  └─ MetricEntry.cs / TraceEntry.cs

Custom/
├─ CustomMetrics.cs
├─ GaugeWrapper.cs

Controllers/
├─ TelemetryPublicApi.cs

LoggerTelemetryBuilder.cs
PostgreSqlMetricExporter.cs
PostgreSqlTraceExporter.cs
```

---

## ✅ Contributions

Pull requests are welcome. Feel free to fork, enhance, and open issues!
