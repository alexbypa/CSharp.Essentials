[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml)
[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)

# 📦 CSharpEssentials.LoggerHelper

## Introduction

A flexible and modular structured logging library for .NET applications based on Serilog. Easily configure logging to Console, File, Email, PostgreSQL, ElasticSearch via simple JSON configuration. Includes automatic placeholder validation and multi-sink orchestration.

It allows you to:

* Send **structured logs** with guaranteed fields like `Action`, `IdTransaction`, `ApplicationName`, `MachineName`
* Dynamically enable **multi-sink** support (Console, File, HTML Email, PostgreSQL, ElasticSearch)
* **Automatically validate** message `{}` placeholders
* Centralize configuration through **LoggerBuilder**, just by editing the **appsettings.json** file

> In common usage scenarios, it is advisable to avoid logging `Information` level events to sinks like Telegram, MSSQL, or PostgreSQL. This practice prevents issues such as HTTP 429 (rate limits) on Telegram and reduces risks of deadlocks or insufficient storage in database systems.
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
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Debug",
        "System": "Debug"
      }
    },
    "SerilogConfiguration": {
      "ApplicationName": "TestApp",
      "SerilogCondition": [
        {"Sink": "ElasticSearch","Level": []},
        {"Sink": "MSSqlServer","Level": []},
        {"Sink": "Email","Level": []},
        {"Sink": "PostgreSQL","Level": ["Information","Warning","Error","Fatal"]},
        {"Sink": "Telegram","Level": []},
        {"Sink": "Console","Level": [ "Information" ]},
        {"Sink": "File","Level": ["Information","Warning","Error","Fatal"]}
      ],
      "SerilogOption": {
        "File": {
          "Path": "D:\\Logs\\ServerDemo",
          "RollingInterval": "Day",
          "RetainedFileCountLimit": 7,
          "Shared": true
        },
        "TelegramOption": {
          "chatId": "xxxxx",
          "Api_Key": "sssss:ttttttttt"
        },
        "PostgreSQL": {
          "connectionString": "<YOUR CONNECTIONSTRING>",
          "tableName": "public",
          "schemaName": "dbo",
          "needAutoCreateTable": true
        },
        "ElasticSearch": {
          "nodeUris": "http://10.0.1.100:9200",
          "indexFormat": "<YOUR INDEX FORMAT>"
        },
        "Email": {
          "From": "<Email Alert>",
          "Port": 587,
          "Host": "<Host EMail>",
          "To": [ "recipient#1", "recipient#2" ],
          "CredentialHost": "<UserName SMTP>",
          "CredentialPassword": "<Password SMTP>"
        },
        "MSSqlServer": {
          "connectionString": "<YOUR CONNECTIONSTRING>",
          "sinkOptionsSection": {
            "tableName": "logs",
            "schemaName": "dbo",
            "autoCreateSqlTable": true,
            "batchPostingLimit": 100,
            "period": "0.00:00:10"
          },
          "columnOptionsSection": {
            "addStandardColumns": [
              "LogEvent"
            ],
            "removeStandardColumns": [
              "Properties"
            ]
          }
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

## 🔥 Register the logger in `Program.cs`

> ℹ️ **Important**: depending on the target framework version, you must configure `LoggerHelper` differently.

If you are using **.NET 6.0**, you must call the configuration directly on the `builder`.
If you are using **.NET 7.0 or later**, you must call it on the `builder.Services`.

Here’s how you should do it:

```csharp
using CSharpEssentials.LoggerHelper;

var builder = WebApplication.CreateBuilder(args);

// Add LoggerHelper configuration
#if NET6_0
    builder.AddLoggerConfiguration();
#else
    builder.Services.AddLoggerConfiguration(builder);
#endif

builder.Services.AddControllers();

var app = builder.Build();

// Logs every HTTP request and response
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapControllers();
app.Run();
```

---

### 🧠 Explanation

| Target Framework | Usage                                               |
| :--------------- | :-------------------------------------------------- |
| .NET 6.0         | `builder.AddLoggerConfiguration();`                 |
| .NET 8.0         | `builder.Services.AddLoggerConfiguration(builder);` |

This ensures full compatibility across different .NET versions.

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

## Database Schema

### PostgreSQL

| Column            | Description                |
| ----------------- | -------------------------- |
| ApplicationName   | Application name           |
| message           | Message content            |
| message\_template | Message template           |
| level             | Log level                  |
| raise\_date       | Log timestamp              |
| exception         | Exception details          |
| properties        | Serialized properties      |
| props\_test       | Additional serialized data |
| MachineName       | Machine name               |
| Action            | Action name                |
| IdTransaction     | Unique transaction ID      |

### SQL Server

| Column          | Type     | Description           |
| --------------- | -------- | --------------------- |
| Message         | nvarchar | Message content       |
| MessageTemplate | nvarchar | Message template      |
| Level           | nvarchar | Log level             |
| TimeStamp       | datetime | Log timestamp         |
| Exception       | nvarchar | Exception details     |
| Properties      | nvarchar | Serialized properties |
| LogEvent        | nvarchar | Serialized log event  |
| IdTransaction   | varchar  | Unique transaction ID |
| MachineName     | varchar  | Machine name          |
| Action          | varchar  | Action name           |

---

## Swagger Example

| Field           | Description                                   |
| --------------- | --------------------------------------------- |
| action          | Action name                                   |
| message         | Text to log                                   |
| applicationName | Application name                              |
| level           | Log level (Information, Warning, Error, etc.) |

---

## HTML Email Screenshot

| Field           | Value                                |
| --------------- | ------------------------------------ |
| Timestamp       | 2025-05-10 17:45:00                  |
| Level           | Error                                |
| IdTransaction   | 7e7b9f65-ed13-439a-852b-18d9d28dd6ec |
| MachineName     | PIXELO30                             |
| Action          | GetUserDetails                       |
| ApplicationName | LoggerHelperDemo                     |
| Message         | Error occurred during request        |

---

## Demo API

Try it live with a demo Web API to validate each log level dynamically:

| Method | Endpoint        | Query Parameters             | Description                                     |
| ------ | --------------- | ---------------------------- | ----------------------------------------------- |
| GET    | `/loggerHelper` | `action`, `message`, `level` | Sends a structured log with the specified level |

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
