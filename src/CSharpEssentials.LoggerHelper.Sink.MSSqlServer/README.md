# CSharpEssentials.LoggerHelper.Sink.MSSqlServer

> SQL Server structured log storage with auto table creation and custom columns for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

**Targets:** `net8.0` · `net9.0` · `net10.0` — Part of the **CSharpEssentials.LoggerHelper** ecosystem. Install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.MSSqlServer
```

---

## Quick Setup — JSON

Add to `appsettings.json`:

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "MSSqlServer", "Levels": ["Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "MSSqlServer": {
        "ConnectionString": "Server=.;Database=Logs;Trusted_Connection=true;TrustServerCertificate=true",
        "TableName": "AppLogs",
        "AutoCreateSqlTable": true,
        "BatchPostingLimit": 100,
        "Period": "0.00:00:10"
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

> **`AutoCreateSqlTable: true`** creates the table on first run if it does not already exist. Set to `false` if you manage schema migrations yourself.

---

## Quick Setup — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("MSSqlServer", LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureMSSqlServer(s => {
        s.ConnectionString  = "Server=.;Database=Logs;Trusted_Connection=true;TrustServerCertificate=true";
        s.TableName         = "AppLogs";
        s.AutoCreateSqlTable = true;
    })
);

var app = builder.Build();
app.UseLoggerHelper();   // ← required
```

---

## What You'll See

Log events are batched and inserted as rows into the configured table. Default columns created by `AutoCreateSqlTable`:

| Column | SQL Type | Notes |
|---|---|---|
| `Id` | `BIGINT IDENTITY` | Primary key |
| `Message` | `NVARCHAR(MAX)` | Rendered log message |
| `MessageTemplate` | `NVARCHAR(MAX)` | Raw template with `{placeholders}` |
| `Level` | `NVARCHAR(128)` | e.g. `Warning`, `Error` |
| `TimeStamp` | `DATETIME` | UTC timestamp |
| `Exception` | `NVARCHAR(MAX)` | Full exception string (nullable) |
| `Properties` | `NVARCHAR(MAX)` | All structured properties as XML |

---

## Custom Columns — Map Log Properties to SQL Columns

Use `AdditionalColumns` to promote any log property into a dedicated, queryable column.

```json
"Sinks": {
  "MSSqlServer": {
    "ConnectionString": "...",
    "TableName": "AppLogs",
    "AdditionalColumns": [
      { "ColumnName": "TenantId",   "DataType": "NVarChar", "DataLength": 100, "AllowNull": true },
      { "ColumnName": "RequestId",  "DataType": "NVarChar", "DataLength": 50,  "AllowNull": true },
      { "ColumnName": "UserId",     "DataType": "NVarChar", "DataLength": 50,  "AllowNull": true }
    ]
  }
}
```

Then populate those properties at runtime using `BeginScope` or `LogContext.PushProperty`:

```csharp
// Option A — BeginScope (preferred for scoped operations)
using (_logger.BeginScope(new Dictionary<string, object?> {
    ["TenantId"]  = "acme",
    ["RequestId"] = HttpContext.TraceIdentifier
}))
{
    _logger.LogWarning("Payment failed for order {OrderId}", orderId);
}

// Option B — LogContext (Serilog-specific)
using (Serilog.Context.LogContext.PushProperty("UserId", userId))
{
    _logger.LogError("Unauthorized access attempt");
}
```

> The `ColumnName` must exactly match the log property name (case-insensitive). The column is created automatically when `AutoCreateSqlTable: true`.

---

## Configuration Options

| Property | Type | Default | Description |
|---|---|---|---|
| `ConnectionString` | `string` | `""` | **Required.** ADO.NET SQL Server connection string. |
| `TableName` | `string` | `"Logs"` | Target table name. |
| `SchemaName` | `string` | `"dbo"` | Table schema. |
| `AutoCreateSqlTable` | `bool` | `true` | Create the table on startup if it does not exist. |
| `BatchPostingLimit` | `int` | `100` | Maximum events per batch INSERT. |
| `Period` | `string` | `"0.00:00:10"` | Flush interval in `d.hh:mm:ss` format. `"0.00:00:10"` = 10 seconds. |
| `AddStandardColumns` | `List<string>?` | `null` | Standard columns to include. Valid values: `Id`, `Message`, `MessageTemplate`, `Level`, `TimeStamp`, `Exception`, `Properties`, `LogEvent`. |
| `RemoveStandardColumns` | `List<string>?` | `null` | Standard columns to exclude (e.g. `["Properties"]` to drop the XML blob). |
| `AdditionalColumns` | `List<AdditionalColumnConfig>?` | `null` | Custom columns mapped from log properties (see above). |

### AdditionalColumnConfig

| Property | Type | Default | Description |
|---|---|---|---|
| `ColumnName` | `string` | `""` | SQL column name — must match the log property name. |
| `DataType` | `string` | `"NVarChar"` | SQL type. Any `SqlDbType` name: `NVarChar`, `Int`, `BigInt`, `DateTime`, `Bit`, etc. |
| `AllowNull` | `bool` | `true` | Whether the column accepts `NULL`. |
| `DataLength` | `int` | `-1` | Column length. `-1` = `MAX`. Use a fixed length (e.g. `100`) for indexed columns. |

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No output at all | `app.UseLoggerHelper()` missing | Add it after `builder.Build()` |
| Table not created | Insufficient DB permissions or `AutoCreateSqlTable: false` | Grant `CREATE TABLE` permission or create the table manually |
| Rows appear with delay | Events are batched — flushed every `Period` or when `BatchPostingLimit` is reached | Reduce `Period` (e.g. `"0.00:00:02"`) or `BatchPostingLimit` for faster writes |
| `AdditionalColumns` column always `NULL` | Log property name doesn't match `ColumnName` | Check casing: `TenantId` in config ↔ `["TenantId"]` in `BeginScope` |
| `TrustServerCertificate` error | SQL Server uses a self-signed certificate | Add `TrustServerCertificate=true` to the connection string |

---

## Links

- [Documentation](https://www.loggerhelper.it)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
