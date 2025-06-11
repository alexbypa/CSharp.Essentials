﻿[![Frameworks](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0-blue)](https://dotnet.microsoft.com/en-us/download)
[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml)
[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)

# 📦 CSharpEssentials.LoggerHelper

## 📑 Table of Contents
* 📘[Introduction](#introduction)
* 🚀[Installation](#installation)
* 🔧[Configuration](#configuration)
* [📨 HTML Email Sink (used with System.Net.smtp)](#html-email-sink)
* [📣 Telegram Sink (used with HttpClient)](#telegram-sink)
* 🐘[PostgreSQL Sink](#postgresql-sink)
* [💾 MS SQL Sink](#ms-sql-sink)
* [🔍 ElasticSearch Sink](#elasticsearch)
* [🔍 Extending LogEvent Properties](#customprop)
* [🧪 Demo API](#demo-api)

## 📘 Introduction<a id='introduction'></a>   [🔝](#table-of-contents)

🚀 **CSharpEssentials.LoggerHelper** is a flexible and modular structured logging library for .NET 6/8/9. It’s powered by Serilog for most sinks, and extended with native support for Telegram (via `HttpClient`) and Email (via `System.Net.Mail`).

⚠️ **Note**: The built-in Serilog Email Sink is currently affected by a blocking issue ([#44](https://github.com/serilog/serilog-sinks-email/issues/44)), so `CSharpEssentials.LoggerHelper` uses `System.Net.Mail` instead for full control and reliability in production.

🧩 Each sink is delivered as an independent NuGet sub-package and dynamically loaded at runtime, acting as a routing hub that writes each log event to a given sink only if the event’s level matches that sink’s configured level (see **Configuration**).

📦 Centralized and intuitive configuration via a single `appsettings.LoggerHelper.json` file with built-in placeholder validation.

🔧 Supports rich structured logs with properties like `IdTransaction`, `ApplicationName`, `MachineName`, and `Action`.

🐞 **Automatically captures both the latest error** (`CurrentError`) **and all initialization errors** in a concurrent `Errors` queue, so you can inspect the single “last” failure or enumerate the full list programmatically, expose them via HTTP headers, logs, etc.  
🔜 **Roadmap:** in the next release we’ll ship a dedicated dashboard package (`CSharpEssentials.LoggerHelper.Dashboard`) to visualize these errors (and your traces/metrics) without ever touching your code.

🔧 Designed for extensibility with plugin support, level-based sink routing, Serilog SelfLog integration, and a safe debug mode.

### 📦 Available Sink Packages


* **Console**: `CSharpEssentials.LoggerHelper.Sink.Console`
* **File**: `CSharpEssentials.LoggerHelper.Sink.File`
* **MSSqlServer**: `CSharpEssentials.LoggerHelper.Sink.MSSqlServer`
* **PostgreSQL**: `CSharpEssentials.LoggerHelper.Sink.PostgreSql`
* **ElasticSearch**: `CSharpEssentials.LoggerHelper.Sink.Elasticsearch`
* **Telegram**: `CSharpEssentials.LoggerHelper.Sink.Telegram` *Used via `HttpClient`*
* **Email**: `CSharpEssentials.LoggerHelper.Sink.Email` *Used via `System.Net.Mail`*

---

## 🚀 Installation <a id='installation'></a>    [🔝](#table-of-contents)
```bash
dotnet add package CSharpEssentials.LoggerHelper
```

```csharp
#if NET6_0
    builder.AddLoggerConfiguration();
#else
    builder.Services.AddLoggerConfiguration(builder);
#endif

// ───────────────────────────────────────────────────────────────
// 🔧 **Register your custom context enricher**:
// This tells LoggerHelper to invoke your `MyCustomEnricher` on every log call,
// so you can inject properties from the ambient context (e.g. controller action,
// HTTP request, user identity, IP address, etc.)
builder.Services.AddSingleton<IContextLogEnricher, MyCustomEnricher>();
// ───────────────────────────────────────────────────────────────

// Optionally enable HTTP middleware logging
app.UseMiddleware<RequestResponseLoggingMiddleware>();
```

## 🔧 Configuration <a id='configuration'></a>    [🔝](#table-of-contents)

### Verifying LoggerHelper Initialization in Your Minimal API Endpoint

After registering LoggerHelper in your pipeline, you can trigger sink loading and check for any initialization errors right in your endpoint handler:

> **Note:**  
> `LoggerRequest` is a custom class that **must** implement `IRequest`.  
> It provides the default log properties:
> - `IdTransaction`  
> - `Action`  
> - `ApplicationName`  
>
> You can extend it with any additional fields you need, e.g. `UserLogged`,`IpAddress`, etc. see : [✨<strong>Extending LogEvent Properties</strong>✨](#customprop)

```csharp
app.MapGet("/users/sync", async ([FromQuery] int page, IUserService service) =>
{
    // 1) Trigger sink loading and log startup event
    loggerExtension<IRequest>.TraceSync(new LoggerRequest(), Serilog.Events.LogEventLevel.Information, null, "Loaded LoggerHelper");

    // 2) Check for a global initialization error
    if (!string.IsNullOrEmpty(LoggerExtension<IRequest>.CurrentError))
    {
        return Results.BadRequest(LoggerExtension<IRequest>.CurrentError);
    }

    // 3) Check for per-sink initialization failures
    if (LoggerExtension<IRequest>.Errors?.Any() == true)
    {
        var details = LoggerExtension<IRequest>.Errors
            .Select(e => $"{e.SinkName}: {e.ErrorMessage}");
        return Results.BadRequest(string.Join("; ", details));
    }

    // 4) Proceed with business logic if all sinks initialized successfully
    var users = await service.SyncUsersAsync(page);
    return Results.Ok(users);
})
.WithName("SyncUsers")
.Produces<List<User>>(StatusCodes.Status200OK);
```

Here’s a **Minimal Configuration Example** that uses **only** the File sink and writes **all** log levels (`Information`, `Warning`, `Error`, `Fatal`):

you need to create **appsettings.LoggerHelper.json** in your project ( on development environment create with the name **appsettings.LoggerHelper.debug.json** )
```json
{
  "Serilog": {
    "SerilogConfiguration": {
      "ApplicationName": "DemoLogger 9.0",
      "SerilogCondition": [
        {
          "Sink": "File",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        }
      ]
    },
    "SerilogOption": {
      "File": {
        "Path": "C:\\Logs\\DemoLogger",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 7,
        "Shared": true
      }
    }
  }
}
```

* **SerilogConfiguration.ApplicationName**: your app’s name.
* **SerilogCondition**: a list of sink-level mappings; here we map **all** levels to the `"File"` sink.
* **SerilogOption.File**: settings specific to the File sink (output folder, rolling interval, retention, etc.).


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
> 📸 Example of a formatted email message:
> ![Email Sample](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper/img/emailsample.png)


### 🔧 File + Email sink example configuration

This configuration writes **every** log event (`Information`, `Warning`, `Error`, `Fatal`) to the **File** sink, but only sends **Email** notifications for high-severity events (`Error` and `Fatal`):

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
      "ApplicationName": "DemoLogger 9.0",
      "SerilogCondition": [
        {
          "Sink": "File",
          "Level": [ "Information", "Warning", "Error", "Fatal" ]
        },
        {
          "Sink": "Email",
          "Level": [ "Error", "Fatal" ]
        }
      ],
      "SerilogOption": {
        "File": {
          "Path": "C:\\Logs\\DemoLogger",
          "RollingInterval": "Day",
          "RetainedFileCountLimit": 7,
          "Shared": true
        },
        "Email": {
          "From": "jobscheduler.pixelo@gmail.com",
          "Port": 587,
          "Host": "smtp.gmail.com",
          "To": "ops-team@example.com",
          "Username": "alerts@example.com",
          "Password": "YOUR_SMTP_PASSWORD",
          "EnableSsl": true,
          "TemplatePath": "Templates/email-template-default.html"
        }
      }
    }
  }
}
```

## 📣 Telegram Sink<a id='telegram-sink'></a>   [🔝](#table-of-contents)
LoggerHelper supports Telegram notifications to alert on critical events.

> ⚠️ **Recommended Levels**: Use only `Error` or `Fatal` to avoid exceeding Telegram rate limits.

### 🔧 Telegram sink example configuration


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

### File + Email + Telegram Sink Example Configuration
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
      "ApplicationName": "DemoLogger 9.0",
      "SerilogCondition": [
        {
          "Sink": "File",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        },
        {
          "Sink": "Email",
          "Level": [
            "Error",
            "Fatal"
          ]
        },
        {
          "Sink": "Telegram",
          "Level": [
            "Error",
            "Fatal"
          ]
        }
      ],
      "SerilogOption": {
        "File": {
          "Path": "C:\\Logs\\DemoLogger",
          "RollingInterval": "Day",
          "RetainedFileCountLimit": 7,
          "Shared": true
        },
        "Email": {
          "From": "jobscheduler.pixelo@gmail.com",
          "Port": 587,
          "Host": "smtp.gmail.com",
          "To": "alexbypa@gmail.com",
          "username": "------",
          "password": "-------------",
          "EnableSsl": true,
          "TemplatePath": "Templates/email-template-default.html"
        },
        "TelegramOption": {
          "chatId": "xxxxxxxxxxx",
          "Api_Key": "wwwwwwwwww:zxxxxxxxxzzzzz"
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
  "tableName": "LogEntry",
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


## 🚀 Extending LogEvent Properties from Your Project<a id='customprop'></a>   [🔝](#table-of-contents)

Starting from version **2.0.9**, you can extend the default log event context by implementing your own **custom enricher**. This allows you to **add extra fields** to the log context and ensure they are included in **all log sinks** (not only in email notifications, but also in any other sink that supports additional fields—especially in the databases, where from version **2.0.8** onwards you can add dedicated columns for these custom properties).
**How to configure it:**

✅ **1️⃣ Register your custom enricher and logger configuration in `Program.cs`**
Before building the app:

```csharp
builder.Services.AddSingleton<IContextLogEnricher, MyCustomEnricher>();
builder.Services.AddloggerConfiguration(builder);
```

✅ **2️⃣ Assign the service provider to `LoggerHelperServiceLocator`**
After building the app:

```csharp
LoggerHelperServiceLocator.Instance = app.Services;
```

✅ **3️⃣ Create your custom enricher class**
Example implementation:

```csharp
public class MyCustomEnricher : IContextLogEnricher {
    public ILogger Enrich(ILogger logger, object? context) {
        if (context is MyCustomRequest req) {
            return logger
                .ForContext("Username", req.Username)
                .ForContext("IpAddress", req.IpAddress);
        }
        return logger;
    }

    public LoggerConfiguration Enrich(LoggerConfiguration configuration) => configuration;
}
```
👉 **Note:**
In addition to the fields already provided by the package (e.g., `MachineName`, `Action`, `ApplicationName`, `IdTransaction`), you can add **custom fields**—such as the **logged-in username** and the **IP address** of the request—using your own properties.

✅ **4️⃣ Use your custom request class in your application**

> **Note:** your custom request class (e.g. `myRequest`) must implement the `ILoggerRequest` interface provided by **LoggerHelper**.

Example usage:

```csharp
var myRequest = new MyCustomRequest {
    IdTransaction = Guid.NewGuid().ToString(),
    Action = "UserLogin",
    ApplicationName = "MyApp",
    MachineName = Environment.MachineName,
    Username = "JohnDoe",
    IpAddress = "192.168.1.100"
};

loggerExtension<MyCustomRequest>.TraceSync(myRequest, LogEventLevel.Information, null, "User login event");
```

✅ **5️⃣ Update your email template to include the new fields**
Example additions:

```html
<tr><th>User Name</th><td>{{Username}}</td></tr>
<tr><th>Ip Address</th><td>{{IpAddress}}</td></tr>
```
✅ **6️⃣ MSSQL and PostgresQL sink Template Customization**
- To add extra fields on table of MSSQL add fields on array **additionalColumns**
- To add extra fields on table of postgre add fields on array **ColumnsPostGreSQL**

🔗 **Download Example**
You can see an example in the [demo controller](https://github.com/alexbypa/CSharp.Essentials/blob/main/Test8.0/Controllers/logger/LoggerController.cs).
Whereas the custom class to generate extra fields can be found [here](https://github.com/alexbypa/CSharp.Essentials/blob/main/Test8.0/Controllers/logger/MyCustomEnricher.cs).

---
## 🧪 Demo API <a id='demo-api'></a>   [🔝](#table-of-contents)

Try it live with full logging and structured output on 📁 [Demo Project](https://github.com/alexbypa/CSharp.Essentials/tree/main/LoggerHelperDemo)

### 📝 appsettings.loggerhelper.json (Development – Debug)

This is the full `appsettings.LoggerHelper.json` used in the demo Minimal API (remember to use appsettings.LoggerHelper.debug.json on development):

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
      "ApplicationName": "DemoLogger 9.0",
      "SerilogCondition": [
        {
          "Sink": "ElasticSearch",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        },
        {
          "Sink": "File",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        },
        {
          "Sink": "Email",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        },
        {
          "Sink": "Telegram",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        },
        {
          "Sink": "PostgreSQL",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        },
        {
          "Sink": "MSSqlServer",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        },
        {
          "Sink": "Console",
          "Level": [
            "Information",
            "Warning",
            "Error",
            "Fatal"
          ]
        }
      ],
      "SerilogOption": {
        "File": {
          "Path": "C:\\Logs\\DemoLogger",
          "RollingInterval": "Day",
          "RetainedFileCountLimit": 7,
          "Shared": true
        },
        "Email": {
          "From": "jobscheduler.pixelo@gmail.com",
          "Port": 587,
          "Host": "your_host",
          "To": "recipient",
          "username": "username_smtp",
          "password": "password_smtp",
          "EnableSsl": true,
          "TemplatePath": "Templates/email-template-default.html"
        },
        "TelegramOption": {
          "chatId": "chatid",
          "Api_Key": "api_key"
        },
        "PostgreSQL": {
          "connectionString": "your_connection",
          "tableName": "LogEntry",
          "schemaName": "public",
          "needAutoCreateTable": true,
          "addAutoIncrementColumn": true,
          "ColumnsPostGreSQL": [
            { "Name": "Message",           "Writer": "Rendered",   "Type": "text1111" },
            { "Name": "MessageTemplate",   "Writer": "Template",   "Type": "text"     },
            { "Name": "Level",             "Writer": "Level",      "Type": "varchar"  },
            { "Name": "TimeStamp",         "Writer": "timestamp",  "Type": "timestamp"},
            { "Name": "Exception",         "Writer": "Exception",  "Type": "text"     },
            { "Name": "Properties",        "Writer": "Properties", "Type": "jsonb"    },
            { "Name": "LogEvent",          "Writer": "Serialized", "Type": "jsonb"    },
            { "Name": "IdTransaction",     "Writer": "Single",     "Property": "IdTransaction",   "Type": "varchar" },
            { "Name": "MachineName",       "Writer": "Single",     "Property": "MachineName",     "Type": "varchar" },
            { "Name": "Action",            "Writer": "Single",     "Property": "Action",          "Type": "varchar" },
            { "Name": "ApplicationName",   "Writer": "Single",     "Property": "ApplicationName", "Type": "varchar" },
            { "Name": "Username",          "Writer": "Single",     "Property": "Username",        "Type": "varchar" },
            { "Name": "IpAddress",         "Writer": "Single",     "Property": "IpAddress",       "Type": "varchar" }
          ]
        },
        "MSSqlServer": {
          "connectionString": "your_connection",
          "sinkOptionsSection": {
            "tableName": "LogEntry",
            "schemaName": "dbo",
            "autoCreateSqlTable": true,
            "batchPostingLimit": 100,
            "period": "0.00:00:10"
          },
          "columnOptionsSection": {
            "addStandardColumns": [
              "LogEvent",
              "Message",
              "MessageTemplate",
              "Level",
              "Exception"
            ],
            "removeStandardColumns": [ "Properties" ]
          },
          "additionalColumns": [
            "IdTransaction",
            "Action",
            "MachineName",
            "ApplicationName",
            "Username",
            "IpAddress"
          ]
        },
        "ElasticSearch": {
          "nodeUris": "endpoint",
          "indexFormat": "indexformat"
        }
      }
    }
  }
}
```

