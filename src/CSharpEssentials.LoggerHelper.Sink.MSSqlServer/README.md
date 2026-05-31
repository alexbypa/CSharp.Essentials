# CSharpEssentials.LoggerHelper.Sink.MSSqlServer

> SQL Server structured log storage with auto table creation and custom columns for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.MSSqlServer
```

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "MSSqlServer", "Levels": ["Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "MSSqlServer": {
        "ConnectionString": "Server=.;Database=Logs;Trusted_Connection=true",
        "TableName": "AppLogs",
        "AutoCreateSqlTable": true
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
    .AddRoute("MSSqlServer", LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureMSSqlServer(s => {
        s.ConnectionString = "Server=.;Database=Logs;Trusted_Connection=true";
        s.TableName = "AppLogs";
        s.AutoCreateSqlTable = true;
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | `""` | SQL Server connection string |
| `TableName` | `string` | `"Logs"` | Target table name |
| `SchemaName` | `string` | `"dbo"` | Table schema |
| `AutoCreateSqlTable` | `bool` | `true` | Automatically create the log table if it doesn't exist |
| `BatchPostingLimit` | `int` | `100` | Maximum number of events per batch insert |
| `Period` | `string` | `"0.00:00:10"` | Batch flush interval (TimeSpan format) |
| `AddStandardColumns` | `List<string>?` | `null` | Standard Serilog columns to include (e.g. `"LogEvent"`, `"Message"`) |
| `RemoveStandardColumns` | `List<string>?` | `null` | Standard columns to exclude (e.g. `"Properties"`) |
| `AdditionalColumns` | `List<AdditionalColumnConfig>?` | `null` | Custom columns mapped from log properties |

### AdditionalColumnConfig

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ColumnName` | `string` | `""` | SQL column name |
| `DataType` | `string` | `"NVarChar"` | SQL data type |
| `AllowNull` | `bool` | `true` | Whether the column accepts NULL |
| `DataLength` | `int` | `-1` | Column length (-1 = MAX) |

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
