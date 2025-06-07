﻿[![Frameworks](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0-blue)](https://dotnet.microsoft.com/en-us/download)
[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml)
[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)

# 📦 CSharpEssentials.LoggerHelper

## 📑 Table of Contents
* 📘[Introduction](#introduction)
* 🚀[Installation](#installation)
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

🧩 Each sink is delivered as an independent NuGet sub-package and dynamically loaded at runtime.

📦 Centralized and intuitive configuration via a single `appsettings.LoggerHelper.json` file with built-in placeholder validation.

🪪 Supports rich structured logs with properties like `IdTransaction`, `ApplicationName`, `MachineName`, and `Action`.

🧠 Automatically captures the latest internal error (`CurrentError`), which can be exposed via HTTP headers or other channels.

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
### Verifying LoggerHelper Initialization in Your Minimal API Endpoint

After registering LoggerHelper in your pipeline, you can trigger sink loading and check for any initialization errors right in your endpoint handler:

```csharp
app.MapGet("/users/sync", async ([FromQuery] int page, IUserService service) =>
{
    // 1) Trigger sink loading and log startup event
    await LoggerExtension<IRequest>.TraceAsync(
        new LoggerRequest(),
        Serilog.Events.LogEventLevel.Information,
        properties: null,
        messageTemplate: "Loaded LoggerHelper"
    );

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

> **Note:** the example above shows the runtime looking for `appsettings.LoggerHelper.debug.json`  
> (in Development) or `appsettings.LoggerHelper.json` (in Production).  
> Make sure you include one of these files in your project output folder, with the exact naming  
> and JSON schema described in the **Configuration** section below.




👉 [Click here to view full usage guide and examples](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)