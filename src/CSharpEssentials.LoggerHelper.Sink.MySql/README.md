# CSharpEssentials.LoggerHelper.Sink.MySql

> MySQL and MariaDB structured log storage with JSON support and custom columns for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

**Targets:** `net8.0` · `net9.0` · `net10.0` — Part of the **CSharpEssentials.LoggerHelper** ecosystem. Install only the sinks you need.

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.MySql.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MySql)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.Sink.MySql.svg?label=downloads&color=brightgreen)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.MySql)

Your structured fields land in **real, queryable columns** — not buried inside a JSON blob — so this sink stays at feature parity with the PostgreSQL sink.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.MySql
```

Built directly on [MySqlConnector](https://mysqlconnector.net/) — fully async, no `MySql.Data` dependency.

**Compatibility:** MySQL 5.7.8+, MySQL 8.x, MariaDB 10.2+

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "MySql", "Levels": ["Warning", "Error", "Fatal"] }
    ],
    "Sinks": {
      "MySql": {
        "ConnectionString": "Server=localhost;Database=logs;Uid=app;Pwd=secret;",
        "TableName": "app_logs",
        "AutoCreateTable": true
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
    .AddRoute("MySql", LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureMySql(m => {
        m.ConnectionString = "Server=localhost;Database=logs;Uid=app;Pwd=secret;";
        m.TableName = "app_logs";
        m.AutoCreateTable = true;
    })
);
```

> The sink name is matched case-insensitively and also accepts `MySQL` and `MariaDB`, so `{ "Sink": "MariaDB" }` routes here too.

---

## What You'll See

Events are batched and inserted as rows into the configured table. With `AutoCreateTable: true` the sink issues this DDL on the first write:

```sql
CREATE TABLE IF NOT EXISTS `app_logs` (
  `Id`               BIGINT AUTO_INCREMENT PRIMARY KEY,
  `ApplicationName`  VARCHAR(255),
  `message`          TEXT,
  `message_template` TEXT,
  `level`            VARCHAR(32),
  `raise_date`       DATETIME(6),
  `exception`        TEXT,
  `properties`       JSON,
  `MachineName`      VARCHAR(255),
  `Action`           VARCHAR(255),
  `IdTransaction`    VARCHAR(255)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

A single `logger.LogError("Payment failed for order {OrderId}", 4711)` becomes:

| level | message | message_template | raise_date | properties |
|---|---|---|---|---|
| `Error` | `Payment failed for order 4711` | `Payment failed for order {OrderId}` | `2025-11-04 09:12:33.412870` | `{"OrderId":4711,"SourceContext":"Api.PaymentService","TraceId":"a3f…"}` |

Because `properties` is a native `JSON` column, structured fields stay queryable even when they have no dedicated column:

```sql
-- Errors of the last hour, grouped by template
SELECT message_template, COUNT(*) AS hits
FROM app_logs
WHERE level IN ('Error','Fatal')
  AND raise_date >= NOW() - INTERVAL 1 HOUR
GROUP BY message_template
ORDER BY hits DESC;

-- Drill into a single structured property inside the JSON column
SELECT raise_date, message
FROM app_logs
WHERE properties->>'$.OrderId' = '4711';
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string` | `""` | MySQL/MariaDB connection string. The sink is skipped with a `SelfLog` warning when empty |
| `TableName` | `string` | `"logs"` | Target table name |
| `AutoCreateTable` | `bool` | `true` | Issue `CREATE TABLE IF NOT EXISTS` on first write |
| `AddAutoIncrementColumn` | `bool` | `true` | Add an `Id BIGINT AUTO_INCREMENT PRIMARY KEY` column when auto-creating |
| `StoreTimestampInUtc` | `bool` | `false` | Write timestamps as UTC instead of local time — **recommended**, see [Timestamps](#timestamps-and-time-zones) |
| `BatchPostingLimit` | `int` | `100` | Events written per batch. Clamped to `1..1000` |
| `Period` | `string` | `"0.00:00:05"` | Flush interval as a `TimeSpan` string. Falls back to 5s if unparsable |
| `Columns` | `List<MySqlColumnConfig>?` | `null` | Custom column definitions (overrides defaults) |

Legacy lowercase JSON keys (`connectionstring`, `tableName`, `autoCreateTable`, `needAutoCreateTable`, `addAutoIncrementColumn`, `ColumnsMySql`) are accepted for compatibility with older configuration files.

### Default Columns

When `Columns` is omitted the sink creates this table automatically:

| Column | Writer | MySQL type | Notes |
|---|---|---|---|
| `ApplicationName` | `Single` | `VARCHAR(255)` | Value of the `ApplicationName` log property |
| `message` | `Rendered` | `TEXT` | Final rendered log message |
| `message_template` | `Template` | `TEXT` | Raw Serilog template with `{placeholders}` |
| `level` | `Level` | `VARCHAR(32)` | e.g. `Information`, `Error` |
| `raise_date` | `Timestamp` | `DATETIME(6)` | Event timestamp, microsecond precision |
| `exception` | `Exception` | `TEXT` | Full exception string (nullable) |
| `properties` | `Serialized` | `JSON` | Full log event as JSON — queryable with `->>` and `JSON_EXTRACT` |
| `MachineName` | `Single` | `VARCHAR(255)` | Host name |
| `Action` | `Single` | `VARCHAR(255)` | Custom `Action` property from scope |
| `IdTransaction` | `Single` | `VARCHAR(255)` | Correlation ID from scope |

The generated table uses `CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci`, so log messages containing emoji or non-BMP characters are stored correctly.

---

## Custom Columns — Replicate or Extend the Default Schema

Use `Columns` to define exactly which columns the sink writes.
When `Columns` is present it **replaces** the default set entirely.

### Example A — Replicating the default schema via JSON

```json
"Sinks": {
  "MySql": {
    "ConnectionString": "Server=localhost;Database=logs;Uid=app;Pwd=secret;",
    "TableName": "app_logs",
    "AutoCreateTable": true,
    "StoreTimestampInUtc": true,
    "Columns": [
      { "Name": "ApplicationName",  "Writer": "Single",     "Type": "VarChar",  "Length": 255, "Property": "ApplicationName" },
      { "Name": "message",          "Writer": "Rendered",   "Type": "Text" },
      { "Name": "message_template", "Writer": "Template",   "Type": "Text" },
      { "Name": "level",            "Writer": "Level",      "Type": "VarChar",  "Length": 32 },
      { "Name": "raise_date",       "Writer": "Timestamp",  "Type": "DateTime" },
      { "Name": "exception",        "Writer": "Exception",  "Type": "Text" },
      { "Name": "properties",       "Writer": "Serialized", "Type": "Json" },
      { "Name": "MachineName",      "Writer": "Single",     "Type": "VarChar",  "Length": 255, "Property": "MachineName" },
      { "Name": "Action",           "Writer": "Single",     "Type": "VarChar",  "Length": 255, "Property": "Action" },
      { "Name": "IdTransaction",    "Writer": "Single",     "Type": "VarChar",  "Length": 255, "Property": "IdTransaction" }
    ]
  }
}
```

### Example B — Custom schema with application-specific properties

Add only the columns you need, including custom properties pushed via `BeginScope`:

```json
"Columns": [
  { "Name": "message",    "Writer": "Rendered",   "Type": "Text" },
  { "Name": "level",      "Writer": "Level",      "Type": "VarChar", "Length": 32 },
  { "Name": "raise_date", "Writer": "Timestamp",  "Type": "DateTime" },
  { "Name": "exception",  "Writer": "Exception",  "Type": "Text" },
  { "Name": "properties", "Writer": "Properties", "Type": "Json" },
  { "Name": "TenantId",   "Writer": "Single",     "Type": "VarChar", "Length": 64, "Property": "TenantId" },
  { "Name": "UserId",     "Writer": "Single",     "Type": "VarChar", "Length": 64, "Property": "UserId" },
  { "Name": "RequestId",  "Writer": "Single",     "Type": "VarChar", "Length": 64, "Property": "RequestId" }
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

### MySqlColumnConfig reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | `""` | Column name in the database table |
| `Writer` | `string` | `"Single"` | How the value is extracted — see table below |
| `Type` | `string` | `"Text"` | MySQL column type — see table below |
| `Property` | `string?` | `null` | Serilog property name for `Single` writer (defaults to `Name`) |
| `Length` | `int` | `255` | Length applied to `VarChar` columns during auto-creation |

**Writer values:**

| Writer | Maps to | Use for |
|---|---|---|
| `Rendered` | Rendered log message | Human-readable message |
| `Template` | Raw message template | Grouping / searching by template |
| `Level` | Log level | `Information`, `Warning`, `Error`, `Fatal` |
| `Timestamp` | Event timestamp | Time-range queries |
| `Exception` | Full exception string | Error analysis |
| `Serialized` | Full log event as JSON | Catch-all blob, includes the rendered message |
| `Properties` | Structured properties only, as JSON | Leaner alternative to `Serialized` |
| `Single` | One named property | Custom scope/enricher values |

**Type values:**

| `Type` | Emitted DDL | Notes |
|---|---|---|
| `Text` | `TEXT` | Up to 64 KB |
| `LongText` | `LONGTEXT` | Up to 4 GB — for very large payloads |
| `Json` | `JSON` | Native JSON on MySQL 5.7.8+; a `LONGTEXT` alias on MariaDB |
| `VarChar` | `VARCHAR(Length)` | `Length` defaults to 255, capped at 16383 |
| `DateTime` / `Timestamp` | `DATETIME(6)` | Microsecond precision, no 2038 limit |
| `Int` | `INT` | |
| `BigInt` | `BIGINT` | |
| `Bool` / `Boolean` | `TINYINT(1)` | |

Any unrecognized value falls back to `TEXT`.

---

## Using an Existing Table

Set `AutoCreateTable: false` and declare `Columns` matching your schema. The sink never alters an existing table.

Table and column names are validated against `^[A-Za-z0-9_]{1,64}$` and quoted with backticks; values are always passed as command parameters, never concatenated into SQL.

---

## Timestamps and Time Zones

MySQL has no time-zone-aware type equivalent to the PostgreSQL `timestamptz`. The sink emits `DATETIME(6)` and leaves the choice of clock to you:

```json
"StoreTimestampInUtc": true
```

| | Behaviour |
|---|---|
| `StoreTimestampInUtc: true` **(recommended)** | Writes `Timestamp.UtcDateTime`. Rows are unambiguous across servers and DST changes |
| `StoreTimestampInUtc: false` (default) | Writes `Timestamp.LocalDateTime` — matches the app server local clock |

If you define the column yourself, prefer `DATETIME(6)` over `TIMESTAMP`: the MySQL `TIMESTAMP` type is limited to the range 1970-01-01 → 2038-01-19.

---

## Migrating from the PostgreSQL Sink

Type mapping for an existing PostgreSQL log table:

| PostgreSQL | MySQL | Note |
|---|---|---|
| `text` | `TEXT` | |
| `character varying(n)` | `VARCHAR(n)` | |
| `timestamp with time zone` | `DATETIME(6)` | Set `StoreTimestampInUtc: true` |
| `jsonb` | `JSON` | Native on MySQL; `LONGTEXT` alias on MariaDB |
| `integer` / `serial` id | `BIGINT AUTO_INCREMENT` | `BIGINT` recommended for log volumes |

The `Writer` values are identical across both sinks, so an existing `Columns` block can be reused by adjusting only the `Type` values.

---

## Behaviour and Reliability

- **Batched and async.** Events are queued and flushed every `Period` or once `BatchPostingLimit` is reached, whichever comes first. The first event is emitted eagerly so you see output immediately at startup.
- **Never throws into your app.** Write failures are reported through the Serilog `SelfLog` and swallowed — a database outage will not take the host process down. Enable diagnostics to see them:

  ```csharp
  Serilog.Debugging.SelfLog.Enable(Console.Error);
  ```

- **Bounded queue.** The internal queue holds 10,000 events. Once it is full, further events are dropped until it drains — memory stays bounded even if the database is unreachable.
- **Each batch runs in a transaction**, so a batch is written in full or not at all.

> **Strict mode and `VARCHAR` lengths.** MySQL runs with `STRICT_TRANS_TABLES` by default since 5.7 and **rejects** over-long values with `ERROR 1406: Data too long for column` instead of truncating. The sink does not truncate either, so the whole batch is lost and reported to `SelfLog`. Size `VARCHAR` columns generously — fully qualified host names and container IDs routinely exceed 50 characters.

---

## Quick Local Setup with Docker

The fastest way to get a MySQL instance to log into:

```bash
docker run -d --name mysql-logs   -e MYSQL_ROOT_PASSWORD=secret   -e MYSQL_DATABASE=logs   -p 3306:3306   mysql:8
```

```json
"MySql": {
  "ConnectionString": "Server=localhost;Port=3306;Database=logs;Uid=root;Pwd=secret;",
  "TableName": "app_logs",
  "AutoCreateTable": true,
  "StoreTimestampInUtc": true
}
```

MariaDB works the same way — swap the image for `mariadb:11` and keep the connection string as is.

Inspect what arrived:

```bash
docker exec -it mysql-logs mysql -uroot -psecret logs -e "SELECT level, message, raise_date FROM app_logs ORDER BY raise_date DESC LIMIT 10;"
```

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No output at all | `app.UseLoggerHelper()` missing | Add it after `builder.Build()` |
| Sink silently does nothing | `ConnectionString` empty — the sink skips itself with a `SelfLog` warning | Set `Sinks.MySql.ConnectionString`, then enable `Serilog.Debugging.SelfLog.Enable(Console.Error)` to see the diagnostics |
| Table not created | `AutoCreateTable: false`, or the user lacks `CREATE` permission | Grant `CREATE` on the schema, or create the table manually and declare matching `Columns` |
| Rows appear with delay | Events are batched — flushed every `Period` or when `BatchPostingLimit` is reached | Lower `Period` (e.g. `"0.00:00:02"`) or `BatchPostingLimit` |
| A custom column is always `NULL` | `Writer: "Single"` property name doesn't match the log property | Check casing: `TenantId` in `Columns` ↔ `["TenantId"]` in `BeginScope` |
| `ERROR 1406: Data too long for column` | MySQL strict mode rejects over-long `VARCHAR` values — the whole batch is lost | Increase `Length` on that column, or switch its `Type` to `Text` |
| Timestamps off by hours | `StoreTimestampInUtc: false` writes the app server local clock | Set `StoreTimestampInUtc: true` |
| `Unknown column 'x' in 'field list'` | Existing table doesn't match the declared `Columns` | Align the `Columns` block with the real schema — the sink never alters an existing table |
| Emoji stored as `?` | Table created outside the sink without `utf8mb4` | `ALTER TABLE app_logs CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;` |
| `SSL Connection error` on MySQL 8 | Server requires TLS the client isn't negotiating | Add `SslMode=Required;` (or `SslMode=None;` for a local dev container) to the connection string |

---

## Links

- [Documentation](https://www.loggerhelper.it)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
