[![Frameworks](https://img.shields.io/badge/.NET-6.0%20%20%7C%208.0-blue)](https://dotnet.microsoft.com/en-us/download)
[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml)
[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)

# 📦 CSharpEssentials.LoggerHelper

## 📜 Version History

* **1.1.2** – Added Middleware
* **1.1.4** – Removed `TraceAsync` on `finally` block of `RequestResponseLoggingMiddleware`
* **1.1.6** – Fixed issues detected by CodeQL
* **1.2.1** – Optimized with test Web API
* **1.2.2** – Optimized `Properties` handling and Email sink
* **1.3.1** – Added compatibility with .NET 6.0
* **2.0.0** – Fixed Email configuration and sink behavior
* **2.0.2** – Optimized HTML template for middleware
* **2.0.4** – Rollback: removed .NET 7.0 support
* **2.0.5** – Fixed `IRequest` interface
* **2.0.6** – Added external email template support
* **2.0.7** - Added addAutoIncrementColumn and ColumnsPostGreSQL on sink postgresQL
* **2.0.8** - Enhanced MSSQL Sink Configuration : Introduced comprehensive management of custom columns for the MSSQL sink.
* **2.0.9** - Breaking Change: Added support for extending log context with custom fields (IRequest extensions)


<a id='table-of-contents'></a>
## 📑 Table of Contents
* 📘[Introduction](#introduction)
* 🚀[Installation](#installation)
* 🐘[PostgreSQL Sink](#postgresql-sink)
* [📣 Telegram Sink](#telegram-sink)
* [📨 HTML Email Sink](#html-email-sink)
* [💾 MS SQL Sink](#ms-sql-sink)
* [🔍 ElasticSearch Sink](#elasticsearch)
* [🧪 Demo API](#demo-api)

## 📘 Introduction<a id='introduction'></a>   [🔝](#table-of-contents)
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

## 🚀 Installation <a id='installation'></a>    [🔝](#table-of-contents)
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

---

### ⚠️ Breaking Changes - Version 2.0.9
✅ **Extended log enrichment with custom fields**

* Starting from version **2.0.9**, you can include **extra log fields** in your consumer application by extending the `IRequest` interface.
* In your custom class, add your desired fields, and they will be available in your logs and in the email template.

You can see an example in the [demo controller](https://github.com/alexbypa/CSharp.Essentials/blob/main/Test8.0/Controllers/logger/LoggerController.cs).
Whereas the custom class to generate extra fields can be found [here](https://github.com/alexbypa/CSharp.Essentials/blob/main/Test8.0/Controllers/logger/MyCustomEnricher.cs).

✅ **Email sink Template Customization**
* In the HTML email template, you can now reference these **custom fields** directly using **placeholders** like:

```html
<tr><th>User Name</th><td>{{Username}}</td></tr>
<tr><th>Ip Address</th><td>{{IpAddress}}</td></tr>
```

✅ **MSSQL and PostgresQL sink Template Customization**
To add extra fields on table of MSSQL add fields on array additionalColumns
To add extra fields on table of postgre add fields on array ColumnsPostGreSQL
---


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
          "needAutoCreateTable": true,          
          "addAutoIncrementColumn": true,
          "ColumnsPostGreSQL": [
              {"Name": "Message","Writer": "Rendered","Type": "text"},
              {"Name": "MessageTemplate","Writer": "Template","Type": "text"},
              {"Name": "Level","Writer": "Level","Type": "varchar"},
              {"Name": "TimeStamp","Writer": "timestamp","Type": "timestamp"},
              {"Name": "Exception","Writer": "Exception","Type": "text"},
              {"Name": "Properties","Writer": "Properties","Type": "jsonb"},
              {"Name": "LogEvent","Writer": "Serialized","Type": "jsonb"},
              {"Name": "IdTransaction","Writer": "Single","Property": "IdTransaction","Type": "varchar"},
              {"Name": "MachineName","Writer": "Single","Property": "MachineName","Type": "varchar"},
              {"Name": "Action","Writer": "Single","Property": "Action","Type": "varchar"},
              {"Name": "ApplicationName","Writer": "Single","Property": "ApplicationName","Type": "varchar"}
          ]

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
          "standardColumns": [ "Message", "MessageTemplate", "Level", "Exception", "TimeStamp" ],
          "columnOptionsSection": {
            "addStandardColumns": ["LogEvent"],
            "removeStandardColumns": ["Properties"]
          }
            "additionalColumns": [
            "IdTransaction",
            "Action",
            "MachineName",
            "ApplicationName",
            "Username",
            "IpAddress"
          ]

        },
        "GeneralConfig": {
          "EnableSelfLogging": false
        }
      }
    }
  }
}
```
## 🐘 PostgreSQL Sink<a id='postgresql-sink'></a>   [🔝](#table-of-contents)

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
> ⚠️ **Note:** When using `ColumnsPostGreSQL`, always enable `SelfLog` during development to detect unsupported or misconfigured column definitions. Invalid types or property names will be silently ignored unless explicitly logged via Serilog’s internal diagnostics.


## 🐘 Telegram Sink<a id='telegram-sink'></a>   [🔝](#table-of-contents)
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

## 📨 HTML Email Sink<a id='html-email-sink'></a>   [🔝](#table-of-contents)
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
## 💾 MS SQL Sink<a id='ms-sql-sink'></a>    [🔝](#table-of-contents)
This sink writes logs to a Microsoft SQL Server table and supports additional context properties out of the box.

### 📦 Changes

✅ **Version 2.0.8**  
* Added **complete management of standard columns** for the MSSQL sink (`standardColumns` option).
* Introduced the new **`additionalColumns`** array, which by default includes the base fields of the package:

  * `IdTransaction`
  * `Action`
  * `MachineName`
  * `ApplicationName`


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
## 🔍 ElasticSearch Sink<a id='elasticsearch'></a>   [🔝](#table-of-contents)

ElasticSearch is ideal for indexing and searching logs at scale. When integrated with **Kibana**, it enables advanced analytics and visualization of log data.

### Benefits

* 🔎 Fast full-text search and filtering
* 📊 Seamless integration with Kibana for dashboards
* 📁 Efficient storage and querying for large volumes of structured logs

### Example Configuration

```json
"ElasticSearch": {
  "nodeUris": "http://<YOUR_IP>:9200",
  "indexFormat": "<YOUR_INDEX>"
}
```

* `nodeUris`: The ElasticSearch node endpoint.
* `indexFormat`: The format or name of the index that will store log entries.

---
## 🧪 Demo API <a id='demo-api'></a>   [🔝](#table-of-contents)


Try live with full logging and structured output:

📁 [Demo Project]

✅ Now available for both **.NET 6.0** and **.NET 8.0**:
- [`/Test6.0`](https://github.com/alexbypa/CSharp.Essentials/tree/main/Test6.0) → Compatible with legacy environments
- [`/Test8.0`](https://github.com/alexbypa/CSharp.Essentials/tree/main/Test8.0) → Optimized for latest runtime features

---
## 🧰 Troubleshooting

Enable Serilog internal diagnostics:

```csharp
SelfLog.Enable(msg => File.AppendAllText("serilog-selflog.txt", msg));
```

## 👤 Author

**Alessandro Chiodo**
📦 [NuGet Package](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper/)
🔗 [GitHub](https://github.com/alexbypa/CSharp.Essentials)
