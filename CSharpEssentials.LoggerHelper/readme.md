[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml)
[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)

# 📦 CSharpEssentials.LoggerHelper

A flexible and modular logging library for .NET applications that simplifies structured logging with multi-sink support. Built on Serilog sinks, it enables dynamic writing to SQL Server, PostgreSQL, Console, File, Email, Telegram, and Elasticsearch based on configurable log levels.

---

## 📚 Table of Contents

* [✨ Features](#-features)
* [🚀 Installation](#-installation)
* [⚙️ Configuration](#%ef%b8%8f-configuration)
* [📌 Log Levels](#-log-levels)
* [🧪 ASP.NET Core Setup](#-aspnet-core-setup)
* [🧑‍💻 Usage Examples](#-usage-examples)
* [🧬 Database Schema](#-database-schema)
* [🔁 Demo API](#-demo-api)
* [🙌 Contributing](#-contributing)
* [📄 License](#-license)
* [👤 Author](#-author)

---

## ✨ Features

* ✅ Multi-sink logging support:

  * Console
  * File
  * SQL Server
  * PostgreSQL
  * Elasticsearch
  * Email
  * Telegram
* ✅ Structured logs with custom properties
* ✅ Sync and async logging
* ✅ Request/response middleware logger
* ✅ Transaction ID, action, machine name
* ✅ Custom levels per sink
* ✅ JSON configuration via `appsettings.LoggerHelper.json`

---

## 🚀 Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper
```

---

## ⚙️ Configuration

Create a file named `appsettings.LoggerHelper.json` in your project root:

```json
{
  "Serilog": {
    "SerilogConfiguration": {
      "SerilogCondition": [
        { "Sink": "Console", "Level": ["Information", "Warning", "Error", "Fatal"] },
        { "Sink": "File", "Level": ["Error", "Fatal"] },
        { "Sink": "PostgreSQL", "Level": ["Error", "Fatal"] },
        { "Sink": "MSSqlServer", "Level": ["Error", "Fatal"] },
        { "Sink": "Telegram", "Level": ["Fatal"] },
        { "Sink": "ElasticSearch", "Level": [] }
      ],
      "SerilogOption": {
        "PostgreSQL": {
          "connectionstring": "Host=localhost;Database=logs;Username=postgres;Password=yourpassword",
          "tableName": "logs",
          "schemaName": "public",
          "needAutoCreateTable": true
        },
        "MSSqlServer": {
          "connectionString": "Server=localhost;Database=Logs;Trusted_Connection=True;",
          "sinkOptionsSection": {
            "tableName": "Logs",
            "schemaName": "dbo",
            "autoCreateSqlTable": true,
            "batchPostingLimit": 50,
            "period": "00:00:30"
          }
        },
        "TelegramOption": {
          "Api_Key": "your-telegram-bot-api-key",
          "chatId": "your-chat-id"
        },
        "File": {
          "Path": "logs"
        },
        "GeneralConfig": {
          "EnableSelfLogging": false
        }
      }
    }
  }
}
```

> ⚠️ **Important:**
> The logger will **only write to a sink** if the `Level` array in `SerilogCondition` contains at least one valid log level (e.g., `"Error"`, `"Warning"`).
> If the `Level` array is empty (e.g., `"Level": []`), **that sink will be ignored**, and **`WriteTo`**\*\* will not be applied\*\*, even if the sink configuration exists.
>
> 🧩 PostgreSQL is preconfigured with a default column mapping for logs. The following columns are used automatically:
> `message`, `message_template`, `level`, `raise_date`, `exception`, `properties`, `props_test`, `machine_name`. No custom mapping is required in the JSON.

---

## 📌 Log Levels

> 🖼️ Example of a Telegram-formatted log message:
> ![Telegram Sample](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper/img/telegramSample.png)
>
> 💬 **Telegram Notice:** When using the Telegram sink, log messages are formatted for human readability, and may include emojis or markdown. For this reason, it's strongly recommended to set the `Level` to only `Error` or `Fatal` to avoid exceeding Telegram's rate limits and to prevent excessive message noise.

> 🛠 **Tip:** Before publishing to production, test each sink you plan to use. You can enable Serilog self-logging to capture internal errors using:
>
> ```csharp
> Serilog.Debugging.SelfLog.Enable(msg =>
>     File.AppendAllText(Path.Combine(logPath, "serilog-selflog.txt"), msg));
> ```
>
> Replace `logPath` with your local or shared log directory. This helps identify misconfigurations or sink loading issues early.

Each sink only receives log levels specified in the `SerilogCondition` array. If a sink's `Level` array is **empty**, that sink will be **ignored entirely**, and no log will be written to it, even if it's configured elsewhere:

| Sink          | Levels                             |
| ------------- | ---------------------------------- |
| Console       | Information, Warning, Error, Fatal |
| File          | Error, Fatal                       |
| PostgreSQL    | Error, Fatal                       |
| MSSqlServer   | Error, Fatal                       |
| Telegram      | Fatal                              |
| Elasticsearch | *(disabled)*                       |

---

## 🧪 ASP.NET Core Setup

### Request/Response Logging Middleware

The LoggerHelper package includes a built-in middleware that logs every incoming HTTP request and outgoing response automatically. It captures:

* HTTP method, path, status code
* Request body (if available)
* Response body (if possible)
* Duration in milliseconds

To enable it, just call:

```csharp
app.UseMiddleware<RequestResponseLoggingMiddleware>();
```

> 📌 This middleware uses `LogEventLevel.Information` by default and is automatically compatible with sinks that accept that level.

Register the logger in `Program.cs`:

```csharp
using CSharpEssentials.LoggerHelper;

var builder = WebApplication.CreateBuilder(args);

// Add LoggerHelper configuration
builder.Services.addloggerConfiguration(builder);
builder.Services.AddControllers();

var app = builder.Build();

// Logs every HTTP request and response
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapControllers();
app.Run();
```

---

## 🧑‍💻 Usage Examples

### 🔹 With request object

```csharp
loggerExtension<MyRequest>.TraceSync(
    request,
    LogEventLevel.Information,
    null,
    "Operation successful: {OperationName}",
    "CreateUser"
);
```

### 🔹 Async logging

```csharp
await loggerExtension<MyRequest>.TraceAsync(
    request,
    LogEventLevel.Error,
    exception,
    "Error during operation: {OperationName}",
    "UpdateUser"
);
```

### 🔹 Without request object

```csharp
loggerExtension<IRequest>.TraceSync(
    null,
    LogEventLevel.Warning,
    null,
    "System warning: {WarningMessage}",
    "Low disk space"
);
```

---

## 🧬 Database Schema

### PostgreSQL

| Column            |
| ----------------- |
| message           |
| message\_template |
| level             |
| raise\_date       |
| exception         |
| properties        |
| props\_test       |
| machine\_name     |

### SQL Server

| Column          |
| --------------- |
| LogEvent (JSON) |
| IdTransaction   |
| MachineName     |
| Action          |

---

## 🔁 Demo API

## Demo API

Try it live with a demo Web API to validate each log level dynamically:

| Method | Endpoint | Query Parameters | Description |
|:---|:---|:---|:---|
| GET | /loggerHelper/info | `action`, `message`, `applicationName`, `level` | Sends a structured log with the specified level |

🔗 [GitHub Repository (Demo)](https://github.com/alexbypa/CSharpEssentials.LoggerHelper/tree/main/CSharpEssentials.LoggerHelper.Demo) 

---

## 🧪 Troubleshooting

### File access denied?

* ❌ If you get `System.IO.IOException` like: *"file is being used by another process"*, make sure:

  * No other process (e.g. text editor, logging library) is locking the file.
  * The file is not open in **append-only exclusive mode**.
* ✅ For self-log output (`serilog-selflog.txt`), ensure that:

  * The target folder exists.
  * The executing process has **write permission** to it.
  * Use `FileShare.ReadWrite` if needed.

### Sink not writing logs?

* ✅ Make sure the `Level` array in `SerilogCondition` is **not empty**.
* ✅ Ensure the sink’s NuGet package is installed **in the main application**, not only in LoggerHelper.
* ✅ Check `serilog-selflog.txt` if enabled — it often reveals silent misconfigurations.

---

## 🙌 Contributing

Contributions, ideas and issues are welcome!
Feel free to open a pull request or discussion on [GitHub](https://github.com/alexbypa/CSharp.Essentials).

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 👤 Author

**Alessandro Chiodo**
📧 GitHub · NuGet · LinkedIn
📦 [NuGet Package](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
