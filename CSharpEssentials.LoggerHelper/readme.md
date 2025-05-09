[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml)
[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)

# 📦 CSharpEssentials.LoggerHelper

A flexible and modular logging library for .NET applications that simplifies structured logging with multi-sink support, including SQL Server, PostgreSQL, Console, File, Email, Telegram, and Elasticsearch.

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

<details>
<summary>Click to expand JSON example</summary>

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

</details>

> ⚠️ **Important:**
> The logger will **only write to a sink** if the `Level` array in `SerilogCondition` contains at least one valid log level (e.g., `"Error"`, `"Warning"`).
> If the `Level` array is empty (e.g., `"Level": []`), **that sink will be ignored**, and **`WriteTo` will not be applied**, even if the sink configuration exists.
>
> 🧩 PostgreSQL is preconfigured with a default column mapping for logs. The following columns are used automatically:
> `message`, `message_template`, `level`, `raise_date`, `exception`, `properties`, `props_test`, `machine_name`. No custom mapping is required in the JSON.

---

## 📌 Log Levels

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

Try it live with a demo Web API to validate each log level:

| Method | Endpoint             | Description        |
| ------ | -------------------- | ------------------ |
| GET    | /LoggerTest/info     | Log: Information   |
| GET    | /LoggerTest/debug    | Log: Debug         |
| GET    | /LoggerTest/warning  | Log: Warning       |
| GET    | /LoggerTest/error    | Log with Exception |
| GET    | /LoggerTest/critical | Log: Critical      |

> GitHub Repository (Demo): [LoggerHelper.DemoApi](https://github.com/alexbypa/CSharpEssentials.DemoApi)
> Try it in Postman or Swagger!

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
