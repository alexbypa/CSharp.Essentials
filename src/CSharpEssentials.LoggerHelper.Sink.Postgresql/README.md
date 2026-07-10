# CSharpEssentials.LoggerHelper.Sink.Postgresql

> PostgreSQL structured log storage with JSONB support and custom columns for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Postgresql
```

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Postgresql", "Levels": ["Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "Postgresql": {
        "ConnectionString": "Host=localhost;Database=logs;Username=app;Password=secret",
        "TableName": "app_logs",
        "NeedAutoCreateTable": true
      }
    }
  }
}
```

```csharp
builder.Services.AddLoggerHelper(builder.Configuration);

var app = builder.Build();
app.UseLoggerHelper();   // ← required: activates sinks and registers middleware
```

## Quick Setup — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Postgresql", LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigurePostgreSql(p => {
        p.ConnectionString = "Host=localhost;Database=logs;Username=app;Password=secret";
        p.TableName = "app_logs";
        p.NeedAutoCreateTable = true;
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | `""` | PostgreSQL connection string |
| `TableName` | `string` | `"logs"` | Target table name |
| `SchemaName` | `string` | `"public"` | Table schema |
| `NeedAutoCreateTable` | `bool` | `true` | Automatically create the log table |
| `AddAutoIncrementColumn` | `bool` | `false` | Add an auto-increment primary key column |
| `Columns` | `List<PostgreSqlColumnConfig>?` | `null` | Custom column definitions (overrides defaults) |

### Default Columns

When `Columns` is omitted the sink creates this table automatically:

| Column | Writer | PostgreSQL type | Notes |
|---|---|---|---|
| `ApplicationName` | `Single` | `Text` | Value of the `ApplicationName` log property |
| `message` | `Rendered` | `Text` | Final rendered log message |
| `message_template` | `Template` | `Text` | Raw Serilog template with `{placeholders}` |
| `level` | `Level` | `Varchar` | e.g. `Information`, `Error` |
| `raise_date` | `Timestamp` | `Timestamp` | UTC timestamp of the event |
| `exception` | `Exception` | `Text` | Full exception string (nullable) |
| `properties` | `Serialized` | `Jsonb` | All structured properties as JSONB — queryable with `->` operator |
| `MachineName` | `Single` | `Text` | Host name |
| `Action` | `Single` | `Text` | Custom `Action` property from scope |
| `IdTransaction` | `Single` | `Text` | Correlation ID from scope |

---

## Custom Columns — Replicate or Extend the Default Schema

Use `Columns` to define exactly which columns the sink writes.
When `Columns` is present it **replaces** the default set entirely.

### Example A — Replicating the default schema via JSON

```json
"Sinks": {
  "Postgresql": {
    "ConnectionString": "Host=localhost;Database=logs;Username=app;Password=secret",
    "TableName": "app_logs",
    "NeedAutoCreateTable": true,
    "Columns": [
      { "Name": "ApplicationName", "Writer": "Single",    "Type": "Text",      "Property": "ApplicationName" },
      { "Name": "message",         "Writer": "Rendered",  "Type": "Text" },
      { "Name": "message_template","Writer": "Template",  "Type": "Text" },
      { "Name": "level",           "Writer": "Level",     "Type": "Varchar" },
      { "Name": "raise_date",      "Writer": "Timestamp", "Type": "Timestamp" },
      { "Name": "exception",       "Writer": "Exception", "Type": "Text" },
      { "Name": "properties",      "Writer": "Serialized","Type": "Jsonb" },
      { "Name": "MachineName",     "Writer": "Single",    "Type": "Text",      "Property": "MachineName" },
      { "Name": "Action",          "Writer": "Single",    "Type": "Text",      "Property": "Action" },
      { "Name": "IdTransaction",   "Writer": "Single",    "Type": "Text",      "Property": "IdTransaction" }
    ]
  }
}
```

### Example B — Custom schema with application-specific properties

Add only the columns you need, including custom properties pushed via `BeginScope`:

```json
"Columns": [
  { "Name": "message",    "Writer": "Rendered",  "Type": "Text" },
  { "Name": "level",      "Writer": "Level",     "Type": "Varchar" },
  { "Name": "raise_date", "Writer": "Timestamp", "Type": "Timestamp" },
  { "Name": "exception",  "Writer": "Exception", "Type": "Text" },
  { "Name": "properties", "Writer": "Serialized","Type": "Jsonb" },
  { "Name": "TenantId",   "Writer": "Single",    "Type": "Text",  "Property": "TenantId" },
  { "Name": "UserId",     "Writer": "Single",    "Type": "Text",  "Property": "UserId" },
  { "Name": "RequestId",  "Writer": "Single",    "Type": "Text",  "Property": "RequestId" }
]
```

Populate the custom properties at runtime with `BeginScope`:

```csharp
using (_logger.BeginScope(new Dictionary<string, object?> {
    ["TenantId"]  = "acme",
    ["UserId"]    = "usr_99",
    ["RequestId"] = HttpContext.TraceIdentifier
}))
{
    _logger.LogWarning("Payment failed for order {OrderId}", orderId);
}
```

> `Property` is required only for `Writer: "Single"` when the column name differs from the Serilog property name. If `Name` and the property name match, you can omit `Property`.

---

### PostgreSqlColumnConfig reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | `""` | Column name in the database table |
| `Writer` | `string` | `"Single"` | How the value is extracted — see table below |
| `Type` | `string` | `"Text"` | PostgreSQL column type: `Text`, `Jsonb`, `Varchar`, `Timestamp` |
| `Property` | `string?` | `null` | Serilog property name for `Single` writer (defaults to `Name`) |

**Writer values:**

| Writer | Maps to | Use for |
|---|---|---|
| `Rendered` | Rendered log message | Human-readable message |
| `Template` | Raw message template | Grouping / searching by template |
| `Level` | Log level | `Information`, `Warning`, `Error`, `Fatal` |
| `Timestamp` | Event UTC timestamp | Time-range queries |
| `Exception` | Full exception string | Error analysis |
| `Serialized` | All properties as JSON | Catch-all JSONB blob |
| `Properties` | All properties (key-value) | Alternative to Serialized |
| `Single` | One named property | Custom scope/enricher values |

---

## Links

- [Documentation](https://www.loggerhelper.it)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)