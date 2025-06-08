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
* 🐘[PostgreSQL Sink](#postgresql-sink)
* [📣 Telegram Sink (used with HttpClient)](#telegram-sink)
* [📨 HTML Email Sink (used with HttpClient)](#html-email-sink)
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
* **Telegram**: *Used via `HttpClient`*
* **Email**: *Used via `System.Net.Mail`*

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
> You can extend it with any additional fields you need, e.g. `UserLogged`, `IpAddress`, etc.
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
### Troubleshooting: Missing appsettings File

If you run a request without the proper appsettings in place, you’ll see an error like this:

![Configuration File 'appsettings.LoggerHelper.debug.json' not found](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/img/badrequest.png)

Here’s a **Minimal Configuration Example** (in English) that uses **only** the File sink and writes **all** log levels (`Information`, `Warning`, `Error`, `Fatal`):

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


👉 [Click here to view full usage guide and examples](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)