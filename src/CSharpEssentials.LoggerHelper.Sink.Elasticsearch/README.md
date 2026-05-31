# CSharpEssentials.LoggerHelper.Sink.Elasticsearch

> Elasticsearch / OpenSearch indexing with auto template registration for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Elasticsearch
```

---

## Quick Setup — JSON

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
builder.Services.AddLoggerHelper(builder.Configuration);
```

## Quick Setup — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Elasticsearch", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureElasticsearch(e => {
        e.NodeUris = "http://localhost:9200";
        e.IndexFormat = "myapp-logs-{0:yyyy.MM.dd}";
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `NodeUris` | `string` | `""` | Elasticsearch node URI(s) |
| `IndexFormat` | `string?` | `null` | Index name format (supports date placeholders) |

The sink automatically registers the index template with Elasticsearch on startup.

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
