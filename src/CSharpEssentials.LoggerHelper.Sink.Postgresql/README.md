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

When `Columns` is not specified, the sink creates these columns automatically:

`ApplicationName`, `message`, `message_template`, `level`, `raise_date`, `exception`, `properties`, `MachineName`, `Action`, `IdTransaction`

### PostgreSqlColumnConfig

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | `""` | Column name |
| `Writer` | `string` | `"Single"` | Writer type: `Rendered`, `Template`, `Level`, `Timestamp`, `Exception`, `Serialized`, `Properties`, `Single` |
| `Type` | `string` | `"Text"` | PostgreSQL type: `Text`, `Jsonb`, `Varchar`, `Timestamp` |
| `Property` | `string?` | `null` | Serilog property name to map |

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
