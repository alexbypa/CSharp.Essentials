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
        "IndexFormat": "myapp-logs-{0:yyyy.MM.dd}",
        "AutoRegisterTemplate": true
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

> **Index template registration is automatic by default.** The sink calls Elasticsearch on startup to register the index template — no manual Kibana/DevTools setup needed. This is a synchronous HTTP call (about 2 s when the node is down); set `"AutoRegisterTemplate": false` to skip it (see [Configuration Options](#configuration-options)).

---

## Quick Setup — Fluent API

```csharp
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Sink.Elasticsearch;
using Serilog.Events;

builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Elasticsearch", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureElasticsearch(e => {
        e.NodeUris    = "http://localhost:9200";
        e.IndexFormat = "myapp-logs-{0:yyyy.MM.dd}";
        e.AutoRegisterTemplate = true;   // default; false skips the startup HTTP call
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

The `IndexFormat` string uses standard .NET date format tokens (`{0:...}`) applied to the event date. When omitted, the Serilog Elasticsearch default (`logstash-{0:yyyy.MM.dd}`) is used.

| Example | Resulting index name |
|---|---|
| `"myapp-logs-{0:yyyy.MM.dd}"` | `myapp-logs-2026.06.01` (daily) |
| `"myapp-logs-{0:yyyy.MM}"` | `myapp-logs-2026.06` (monthly) |
| `"myapp-logs"` | `myapp-logs` (no date — single index, never rolls) |

---

## OpenSearch Compatibility

This sink targets the Elasticsearch 7.x REST API, which OpenSearch also exposes — use the same configuration, pointing `NodeUris` at your OpenSearch node:

```json
"NodeUris": "http://localhost:9200"
```

OpenSearch exposes the same REST API as Elasticsearch 7.x on port 9200 by default.

---

## Configuration Options

| Property | Type | Default | Description |
|---|---|---|---|
| `NodeUris` | `string` | `""` | **Required.** Elasticsearch node URL. For HTTPS or authentication include them in the URI: `"https://user:pass@es-host:9243"`. |
| `IndexFormat` | `string?` | `null` | Index name format with optional date placeholder `{0:...}`. When `null` Serilog uses its own default (`logstash-{0:yyyy.MM.dd}`). |
| `AutoRegisterTemplate` | `bool` | `true` | Registers the index template with a synchronous HTTP call at startup (idempotent). Set `false` to skip it, e.g. when the template is managed elsewhere or the node may be down at startup. Legacy JSON keys `nodeUris`, `indexFormat` and `autoRegisterTemplate` are also accepted. |

> The sink name is matched case-insensitively; the v4 spelling `ElasticSearch` is accepted as an alias for the route and the `Sinks` section.

> **Startup validation.** An empty `NodeUris` (or a missing `Elasticsearch` section) makes the sink fail to configure with an `InvalidOperationException`; LoggerHelper records it as not configured (shown as FAILED in the Dashboard/MCP) and the other sinks keep working.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No output at all | `app.UseLoggerHelper()` missing | Add it after `builder.Build()` |
| `401 Unauthorized` | Elasticsearch 8.x security is enabled by default | Include credentials in `NodeUris`: `"https://elastic:password@localhost:9200"` |
| `connection refused` on port 9200 | Elasticsearch is not running or wrong port | Start Elasticsearch and verify the node URL |
| Index not visible in Kibana | `IndexFormat` date mismatch or wrong data view pattern | Check the index name in Elasticsearch: `GET /_cat/indices?v` |
| Template registration error at startup | Insufficient Elasticsearch permissions | Grant `manage_index_templates` privilege to the connecting user, or set `AutoRegisterTemplate: false` |
| Startup takes ~2 s longer when Elasticsearch is down | Template registration is a synchronous HTTP call | Set `AutoRegisterTemplate: false` |
| Sink shows FAILED in Dashboard/MCP, no logs sent | `NodeUris` empty or `Elasticsearch` section missing | Set `Sinks.Elasticsearch.NodeUris` |
| `No connection could be made` on WSL | `localhost` resolves to IPv6, Docker only listens on IPv4 | Use `http://127.0.0.1:9200` instead of `http://localhost:9200` in `NodeUris` |
| Index created but no documents | Elasticsearch 8.x version detection fails with the Serilog sink | This sink sets `DetectElasticsearchVersion = false` and `AutoRegisterTemplateVersion = ESv7` internally — no action needed on your side |

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

> **WSL users:** if the connection times out, use `http://127.0.0.1:9200` instead of `http://localhost:9200`.
> WSL can route `localhost` to an IPv6 address that Docker does not expose, causing silent failures.

---

## Viewing Logs in Kibana — Step by Step

Once your app is running and sending logs, follow these steps to visualize them.

### Step 1 — Verify the index exists

1. Open Kibana at `http://localhost:5601`
2. Open the left menu (☰) → **Stack Management** (gear icon at the bottom)
3. Under **Data** → click **Index Management**
4. Look for your index (e.g. `myapp-logs-2026.07.03`) — the **Docs count** column must be greater than zero

If the index is missing, the sink is not writing. Check `NodeUris` and confirm `app.UseLoggerHelper()` is called.

### Step 2 — Create a Data View

A Data View is how Kibana maps an index pattern to its query engine.

1. Still in **Stack Management** → under **Kibana** → click **Data Views**
2. Click **Create data view**
3. In the **Index pattern** field enter: `myapp-logs-*` (the `*` wildcard covers all daily indices)
4. Kibana confirms the matched indices in real time
5. In the **Timestamp field** dropdown select **`@timestamp`**
6. Click **Save data view**

### Step 3 — Browse logs in Discover

1. Open the left menu (☰) → **Analytics** → **Discover**
2. Select your data view from the dropdown in the top-left (e.g. `myapp-logs-*`)
3. Set the **time filter** in the top-right to *Last 15 minutes* (or the period when your app ran)
4. Log events appear in the table — click any row to expand the full JSON document

![Kibana Discover — LoggerHelper logs](../../img/kibana-discover-loggerhelper.png)

Each document exposes all structured properties set via `BeginScope` or call-site parameters
(e.g. `fields.ApplicationName`, `fields.RenderedMessage`, `fields.MachineName`) and is fully
searchable with KQL: `fields.ApplicationName : "MyApp" and level : "Error"`.

---

## Links

- [Documentation](https://www.loggerhelper.it)
- [Elasticsearch](https://www.elastic.co/elasticsearch)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)