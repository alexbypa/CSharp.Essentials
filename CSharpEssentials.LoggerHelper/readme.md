[![Frameworks](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0-blue)](https://dotnet.microsoft.com/en-us/download)
[![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml)
[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)

# 📦 CSharpEssentials.LoggerHelper

## 🚀 Why CSharpEssentials.LoggerHelper?

- **🔌 Modular Architecture**  
  Instead of bundling dozens of sinks into one monolithic library, each sink lives in its own NuGet sub‐package (e.g., `CSharpEssentials.LoggerHelper.Sink.File`, `...Sink.MSSqlServer`, and so on). You install only what you need.

- **⚡️ Dynamic Sink Loading**  
  Our **`TolerantPluginLoadContext`** ensures that sinks load “on the fly” at runtime without crashing your app—missing dependencies or version mismatches? No worries. Other sinks continue to work flawlessly.

- **📄 Centralized Configuration**  
  Manage all your sinks and log‐levels in a single JSON file (`appsettings.LoggerHelper.json`). Clean, intuitive, and flexible.

- **🛠️ “CurrentError” Error Tracking**  
  A brand‐new `CurrentError` static property captures the last exception thrown inside the library. Perfect for production scenarios where you want to expose the most recent failure (for example, inserting it into an HTTP header) :
```cs
          if (!string.IsNullOrEmpty(loggerExtension<ProviderRequest>.CurrentError))
            HttpContext.Response.Headers["loggerExtension.CurrentError"] = loggerExtension<ProviderRequest>.CurrentError;
```
- **📈 Structured, Level‐Based Routing**  
  Direct logs to one or many sinks based on level (`Information`, `Warning`, `Error`, etc.). You decide what goes where—and it’s easy to change on the fly.

- **🔀 Infinite Extensibility**  
  Write your own `ISinkPlugin` implementations, drop them in a folder, and CSharpEssentials.LoggerHelper will discover and register them automatically.

- **💡 SelfLog Support**  
  Serilog’s internal SelfLog writes to a file you specify so you never miss a diagnostic message if something goes wrong in your logging pipeline.

---

## 🆕 What’s New in **v3.0.5**

Version **3.0.5** is a major milestone! Highlights:

1. **Dynamic Loading Revamped**  
   - Introduces `TolerantPluginLoadContext`—a custom `AssemblyLoadContext` that quietly ignores missing dependencies.  
   - No more “Could not load assembly” exceptions when a plugin references a missing formatter or helper library. Other sinks keep on working smoothly.

2. **`CurrentError` & Full Error List**  
   - Capture and store the last exception message (`Exception.Message`) that occurred inside LoggerHelper via `LoggerExtension<YourContext>.CurrentError`.  
   - **Roadmap:** we will surface **all** initialization errors in a dedicated dashboard (`CSharpEssentials.LoggerHelper.Dashboard`), so you can see both the single “last” failure and the complete list of errors in one place.

3. **Quality‐of‐Life Improvements**  
   - Updated to support .NET 8.0 (and .NET 6.0).  
   - Minor bug fixes, performance optimizations, and improved documentation links.

> **NOTE:** If you currently reference older versions of Serilog sinks in your project, double‐check the [Known Issues](#known‐issues) section below before upgrading.

---
## Available Sink Packages

- **Console**: `CSharpEssentials.LoggerHelper.Sink.Console`  
- **File**: `CSharpEssentials.LoggerHelper.Sink.File`  
- **MSSqlServer**: `CSharpEssentials.LoggerHelper.Sink.MSSqlServer`  
- **PostgreSQL**: `CSharpEssentials.LoggerHelper.Sink.PostgreSql`  
- **ElasticSearch**: `CSharpEssentials.LoggerHelper.Sink.Elasticsearch`  
- **Telegram**: `Used via HttpClient`  
- **Email**: `Used via System.Net.Mail`  

---

## 🚀 Basic Usage

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)
