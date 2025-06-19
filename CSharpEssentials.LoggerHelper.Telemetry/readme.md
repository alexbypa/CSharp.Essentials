# CSharpEssentials.LoggerHelper.Sink.Telemetry

Plug-and-play extension that integrates **Serilog** and **OpenTelemetry** with direct **PostgreSQL export** of:

- 📊 Metrics (HTTP, GC, ASP.NET, custom)
- 🧵 Traces (activities, spans)
- 🪵 Logs (linked by `trace_id`)

---

## 📦 Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Telemetry
```

---

## ⚙️ Configuration

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

## 🚀 What It Does

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

## ✨ Custom Metrics

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

## 🔗 Full Correlation

Every request automatically links:
```
Request → Trace → Metric → Log
```
via shared `trace_id`.

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
