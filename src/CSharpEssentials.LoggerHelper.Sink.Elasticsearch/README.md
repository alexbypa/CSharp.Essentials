# CSharpEssentials.LoggerHelper.Sink.Elasticsearch

> Elasticsearch / OpenSearch indexing with automatic index template registration for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

**Targets:** `net8.0` · `net9.0` · `net10.0` — Part of the **CSharpEssentials.LoggerHelper** ecosystem. Install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Elasticsearch
```

---

## Quick Setup — JSON

Add to `appsettings.json`:

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Elasticsearch", "Levels": ["Information", "Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "Elasticsearch": {
        "NodeUris": "http://localhost:9200",
        "IndexFormat": "myapp-logs-{0:yyyy.MM.dd}"
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

> **Index template registration is automatic.** The sink calls Elasticsearch on startup to register the index template — no manual Kibana/DevTools setup needed.

---

## Quick Setup — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Elasticsearch", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureElasticsearch(e => {
        e.NodeUris    = "http://localhost:9200";
        e.IndexFormat = "myapp-logs-{0:yyyy.MM.dd}";
    })
);

var app = builder.Build();
app.UseLoggerHelper();   // ← required
```

---

## What You'll See

Each log event is indexed as a JSON document:

```json
{
  "@timestamp": "2026-06-01T14:23:01.123Z",
  "level": "Information",
  "message": "Order 42 placed by usr_99",
  "messageTemplate": "Order {OrderId} placed by {UserId}",
  "fields": {
    "OrderId": 42,
    "UserId": "usr_99",
    "ApplicationName": "MyApp"
  }
}
```

Documents land in the index matching your `IndexFormat` (e.g. `myapp-logs-2026.06.01`). Query them in **Kibana**, **OpenSearch Dashboards**, or with the Elasticsearch REST API.

---

## Index Format

The `IndexFormat` string uses standard .NET date format tokens applied to the current UTC date:

| Example | Resulting index name |
|---|---|
| `"myapp-logs-{0:yyyy.MM.dd}"` | `myapp-logs-2026.06.01` (daily) |
| `"myapp-logs-{0:yyyy.MM}"` | `myapp-logs-2026.06` (monthly) |
| `"myapp-logs"` | `myapp-logs` (no date — single index, never rolls) |

---

## OpenSearch Compatibility

This sink is fully compatible with **OpenSearch** — use the same configuration, pointing `NodeUris` at your OpenSearch node:

```json
"NodeUris": "http://localhost:9200"
```

OpenSearch exposes the same REST API as Elasticsearch 7.x on port 9200 by default.

---

## Configuration Options

| Property | Type | Default | Description |
|---|---|---|---|
| `NodeUris` | `string` | `""` | **Required.** Elasticsearch node URL. For HTTPS or authentication include them in the URI: `"https://user:pass@es-host:9243"`. |
| `IndexFormat` | `string?` | `null` | Index name format with optional date placeholder `{0:...}`. When `null` Serilog uses its own default. |

> **`autoRegisterTemplate` is always `true`** — the sink registers the index template automatically on startup. This is not user-configurable but can be safely repeated (idempotent).

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No output at all | `app.UseLoggerHelper()` missing | Add it after `builder.Build()` |
| `401 Unauthorized` | Elasticsearch 8.x security is enabled by default | Include credentials in `NodeUris`: `"https://elastic:password@localhost:9200"` |
| `connection refused` on port 9200 | Elasticsearch is not running or wrong port | Start Elasticsearch and verify the node URL |
| Index not visible in Kibana | `IndexFormat` date mismatch or wrong data view pattern | Check the index name in Elasticsearch: `GET /_cat/indices?v` |
| Template registration error at startup | Insufficient Elasticsearch permissions | Grant `manage_index_templates` privilege to the connecting user |

---

## Quick Local Setup with Docker

```bash
# Elasticsearch
docker run -d --name elasticsearch \
  -e "discovery.type=single-node" \
  -e "xpack.security.enabled=false" \
  -p 9200:9200 \
  docker.elastic.co/elasticsearch/elasticsearch:8.13.0

# Kibana (optional — for log visualization)
docker run -d --name kibana \
  --link elasticsearch:elasticsearch \
  -p 5601:5601 \
  docker.elastic.co/kibana/kibana:8.13.0
```

Then set `NodeUris` to `"http://localhost:9200"` and open Kibana at `http://localhost:5601`.

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [Elasticsearch](https://www.elastic.co/elasticsearch)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
