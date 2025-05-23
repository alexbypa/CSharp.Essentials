[![Frameworks](https://img.shields.io/badge/.NET-6.0%20%20%7C%208.0-blue)](https://dotnet.microsoft.com/en-us/download)
[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml)
[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)

# 📦 CSharpEssentials.LoggerHelper

## 📑 Table of Contents
* 📘[Introduction](#introduction)
* 🚀[Installation](#installation)
* [🐘 PostgreSQL Sink](#postgresql-sink)
* [📣 Telegram Sink](#telegram-sink)
* [📨 HTML Email Sink](#html-email-sink)
* [💾 MS SQL Sink](#ms-sql-sink)
* [🧪 Demo API](#demo-api)

## 📊 Summary Table

| Sink            | Configuration Key   | Required Settings                 | Additional Notes                                                        |
| --------------- | ------------------- | --------------------------------- | ----------------------------------------------------------------------- |
| PostgreSQL Sink | `PostgreSql`        | ConnectionString, TableName       | Logs are mapped automatically to predefined columns                     |
| Email Sink      | `LoggerHelperEmail` | From, Host, Port, To, Credentials | Since 2.0.0 the section has been renamed from `Email`                   |
| Telegram Sink   | `Telegram`          | BotToken, ChatId                  | Use [https://t.me/BotFather](https://t.me/BotFather) to create your bot |
| File Sink       | `File`              | Path                              | Standard Serilog file sink                                              |
| Console Sink    | `Console`           | Level                             | No extra setup required                                                 |

👉 For more examples, refer to the detailed configuration sections below.

## 📘 Introduction<a id='introduction'></a>
**LoggerHelper** is a flexible and modular structured logging library for .NET (6.0/8.0) applications based on Serilog. It enables structured, multi-sink logging through a plug-and-play approach.

### 🔑 Key Benefits:

* ✅ Structured logs: `Action`, `IdTransaction`, `ApplicationName`, `MachineName`
* ✅ Multi-sink: Console, File, Email (HTML), PostgreSQL, ElasticSearch, Telegram
* ✅ Placeholder validation: avoids runtime `{}` mismatch errors
* ✅ One config file: `appsettings.LoggerHelper.json`
* ✅ Modular integration via `LoggerBuilder`

> ⚠️ **Important for developers:** In development mode, LoggerHelper **automatically uses** `appsettings.LoggerHelper.debug.json`. This allows safe testing without affecting production settings.

```csharp
#if DEBUG
    .AddJsonFile("appsettings.LoggerHelper.debug.json")
#else
    .AddJsonFile("appsettings.LoggerHelper.json")
#endif
```

## 🚀 Installation <a id='installation'></a>
```bash
dotnet add package CSharpEssentials.LoggerHelper
```

## ⚙️ Configuration

The full configuration JSON can be found in the original README. Important:

* Define the `SerilogCondition` for each sink with the desired `Level`
* If `Level` is empty, the sink is ignored

## ⚙️ General Setup

To activate LoggerHelper and enable request/response logging, configure your application in `Program.cs` as follows:

```csharp
#if NET6_0
    builder.AddLoggerConfiguration();
#else
    builder.Services.AddLoggerConfiguration(builder);
#endif
```

Enable HTTP middleware logging:

```csharp
app.UseMiddleware<RequestResponseLoggingMiddleware>();
```

Example `appsettings.LoggerHelper.json` configuration (⚠️ or `appsettings.LoggerHelper.debug.json` during development):

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
        {"Sink": "Telegram","Level": ["Fatal"]},
        {"Sink": "Console","Level": [ "Information" ]},
        {"Sink": "File","Level": ["Information","Warning","Error","Fatal"]}
      ],
      "SerilogOption": {
        "File": {
          "Path": "D:\Logs\ServerDemo",
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
          "username": "<UserName SMTP>",
          "password": "<Password SMTP>"
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
            "addStandardColumns": ["LogEvent"],
            "removeStandardColumns": ["Properties"]
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
[## 🐘 PostgreSQL Sink](#postgresql-sink)

LoggerHelper supports logging to PostgreSQL with optional custom schema definition.

* If `ColumnsPostGreSQL` is **not set**, the following default columns will be created and used:

  * `message`, `message_template`, `level`, `raise_date`, `exception`, `properties`, `props_test`, `machine_name`
* If `ColumnsPostGreSQL` is defined, LoggerHelper will use the exact fields provided.
* Setting `addAutoIncrementColumn: true` will add an `id SERIAL PRIMARY KEY` automatically.

Example configuration:

```json
"PostgreSQL": {
  "connectionString": "...",
  "tableName": "Logs",
  "schemaName": "public",
  "addAutoIncrementColumn": true,
  "ColumnsPostGreSQL": [
    { "Name": "Message", "Writer": "Rendered", "Type": "text" },
    { "Name": "Level", "Writer": "Level", "Type": "varchar" }
  ]
}
```
## 🧪 PostgreSQL Table Structure

If custom `ColumnsPostGreSQL` is defined, logs will include all specified fields.


> 🧩 Tip: PostgreSQL sink is ideal for deep analytics and long-term log storage.

## 📣 Telegram Sink
[#telegram-sink](#telegram-sink)

LoggerHelper supports Telegram notifications to alert on critical events.

> ⚠️ **Recommended Levels**: Use only `Error` or `Fatal` to avoid exceeding Telegram rate limits.

### 🛠 Example Configuration

```json
"TelegramOption": {
  "chatId": "<YOUR_CHAT_ID>",
  "Api_Key": "<YOUR_BOT_API_KEY>"
}
```

To configure a Telegram Bot:

1. Open Telegram and search for [@BotFather](https://t.me/BotFather)
2. Create a new bot and copy the API token
3. Use [https://api.telegram.org/bot<YourBotToken>/getUpdates](https://core.telegram.org/bots/api#getupdates) to get your chat ID after sending a message to the bot

> 📸 Example of a formatted Telegram message:
> ![Telegram Sample](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper/img/telegramSample.png)

## 💡 Usage Examples

```csharp
_logger.TraceSync(
    new Request {
        IdTransaction = Guid.NewGuid().ToString(),
        Action = "SampleAction",
        ApplicationName = "MyApp"
    },
    LogEventLevel.Information,
    null,
    "Sample log message: {Parameter}",
    123
);
```

Or async:

```csharp
await _logger.TraceAsync(
    request,
    LogEventLevel.Error,
    ex,
    "Something failed: {ErrorMessage}",
    ex.Message
);
```

## 📨 HTML Email Sink

---

## ⚠️ Version 2.0.0 - Breaking Change

> Starting from version **2.0.0**, the `Email` configuration section has been **renamed**.
>
> If you are upgrading from `1.x.x`, you MUST update your `appsettings.LoggerHelper.json`.

Old (before 2.0.0):

```json
"Email": {
  "From": "...",
  "Host": "...",
  "Port": 587,
  "To": ["..."],
  "CredentialHost": "...",
  "CredentialPassword": "..."
}
```

New (since 2.0.0):

```json
"Email": {
  "From": "...",
  "Host": "...",
  "Port": 587,
  "To": "...",
  "username": "...",
  "password": "...",
  "EnableSsl": true
}
```

## 🚨 Why Email Handling Changed

Starting from version 2.0.0, LoggerHelper **no longer uses** the standard [Serilog.Sinks.Email](https://github.com/serilog/serilog-sinks-email) for sending emails.

**Reason:**
The official Serilog Email Sink does not support custom body formatting (HTML templates, structured logs, color coding, etc).
It only supports plain text messages generated via `RenderMessage()`, without the ability to control the message content.

🔎 See discussion: [GitHub Issue - serilog/serilog-sinks-email](https://github.com/serilog/serilog-sinks-email/issues/44)

**What changed:**

* LoggerHelper now uses a **custom internal SMTP sink**: `LoggerHelperEmailSink`.
* This allows sending fully customized **HTML-formatted emails**.
* Supports dynamic coloring based on log level (Information, Warning, Error).
* Supports secure SMTP with SSL/TLS.

✅ No third-party dependencies added.
✅ Full control over email appearance and content.

Since v2.0.0, LoggerHelper no longer uses `Serilog.Sinks.Email`. It ships with `LoggerHelperEmailSink`, allowing:

* ✅ Full HTML customization via external template
* ✅ Dynamic styling based on log level
* ✅ Secure SMTP (SSL/TLS)

Example HTML placeholders:

```html
{{Timestamp}}, {{Level}}, {{Message}}, {{Action}}, {{IdTransaction}}, {{MachineName}}, {{ApplicationName}}, {{LevelClass}}
```

### 🖌️ Email Template Customization (optional)

LoggerHelper allows you to customize the **HTML structure and appearance** of the email body.
You can provide an external `.html` file with placeholders like:

```html
{{Timestamp}}, {{Level}}, {{Message}}, {{Action}}, {{IdTransaction}}, {{MachineName}}, {{ApplicationName}}, {{LevelClass}}
```

Then, in the `appsettings.LoggerHelper.json` configuration file, set:

```json
"LoggerHelper": {
  "SerilogOption": {
    "Email": {
      ...
      "TemplatePath": "Templates/email-template-default.html"
    }
  }
}
```

If the file is missing or invalid, LoggerHelper will **fall back to the internal default template**, ensuring backward compatibility.
---
## 💾 MS SQL Sink

This sink writes logs to a Microsoft SQL Server table and supports additional context properties out of the box.

### Configuration Example

```json
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
}
```

### Explanation

* `connectionString`: Full connection string to the SQL Server instance.
* `tableName`: Name of the table that will receive log entries.
* `schemaName`: Schema to use for the log table (default is `dbo`).
* `autoCreateSqlTable`: If true, the log table will be created automatically if it does not exist.
* `batchPostingLimit`: Number of log events to post in each batch.
* `period`: Interval for batching log posts.
* `addStandardColumns`: Additional default Serilog columns to include (e.g., `LogEvent`).
* `removeStandardColumns`: Columns to exclude from the standard set.

### Additional Columns

This sink automatically adds the following custom fields to each log:

* `IdTransaction`: a unique identifier for tracking a transaction.
* `MachineName`: name of the server or machine.
* `Action`: custom action tag if set via `Request.Action`.
* `ApplicationName`: name of the application logging the message.

---

## 🧪 Demo API

Try live: [Demo Project](https://github.com/alexbypa/CSharpEssentials.LoggerHelper/tree/main/CSharpEssentials.LoggerHelper.Demo)

### Example Endpoint

```http
GET /loggerHelper?action=Login&message=Start login&level=Information
```

## 🧰 Troubleshooting

Enable Serilog internal diagnostics:

```csharp
SelfLog.Enable(msg => File.AppendAllText("serilog-selflog.txt", msg));
```

## 👤 Author

**Alessandro Chiodo**
📦 [NuGet Package](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
🔗 [GitHub](https://github.com/alexbypa/CSharp.Essentials)
