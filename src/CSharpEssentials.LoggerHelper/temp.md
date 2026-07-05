# CSharpEssentials.LoggerHelper 📦⚡

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**CSharpEssentials.LoggerHelper** is the ultimate plug-and-play logging hub for modern .NET applications. Built on top of the robust Serilog ecosystem, it eliminates boilerplate configuration and provides a seamless, JSON-driven setup for multiple sinks, advanced routing, and data masking.

Stop wrestling with complex logging configurations. Just plug in the sinks you need, configure them in your `appsettings.json`, and you are ready for production.

---

## ✨ Why choose LoggerHelper? (Core Features)

Developers switch to **CSharpEssentials.LoggerHelper** because it solves complex enterprise logging requirements out of the box:

*   🔀 **Dynamic Sink Routing:** Route specific logs to specific sinks based on log levels, namespaces, or custom properties directly from JSON. No code changes required.
*   🛡️ **Sensitive Data Masking:** GDPR and PCI-DSS compliance made easy. Automatically mask passwords, tokens, and PII in your log events.
*   🚦 **Sink Throttling:** Prevent log spamming and reduce costs by throttling repetitive errors or high-volume events.
*   📊 **Live Dashboard:** Real-time, built-in UI to monitor your log streams and performance metrics.
*   🤖 **AI-Ready (MCP Protocol):** Expose your logs to AI assistants (like Claude or Cursor) using the Model Context Protocol for automated troubleshooting.

---

## 🚀 Quick Start

Get your structured logging up and running in seconds.

### 1. Install the Core Package
```bash
dotnet add package CSharpEssentials.LoggerHelper
```

### 2. Register the Logger (`Program.cs`)
```csharp
using CSharpEssentials.LoggerHelper;

var builder = WebApplication.CreateBuilder(args);

// One line to initialize the entire logging hub
builder.AddCSharpEssentialsLogger(); 

var app = builder.Build();
app.Run();
```

### 3. Basic Configuration (`appsettings.json`)
```json
{
  "LoggerHelper": {
    "MinimumLevel": "Information",
    "Routes": [
      {
        "Name": "Console",
        "Enabled": true
      }
    ]
  }
}
```

---

## 🔌 Supported Sinks (Plug-and-Play Modules)

To keep your application lightweight, **install only the sinks you need**. 
Click on any sink below for detailed, line-by-line JSON configuration instructions.

| Sink Module | NuGet Package | Documentation |
| :--- | :--- | :--- |
| **Console** 💻 | `CSharpEssentials.LoggerHelper.Sink.Console` | [📖 Read Docs](./src/CSharpEssentials.LoggerHelper.Sink.Console/README.md) |
| **File** 📄 | `CSharpEssentials.LoggerHelper.Sink.File` | [📖 Read Docs](./src/CSharpEssentials.LoggerHelper.Sink.File/README.md) |
| **MS SQL Server** 🗄️ | `CSharpEssentials.LoggerHelper.Sink.MSSqlServer` | [📖 Read Docs](./src/CSharpEssentials.LoggerHelper.Sink.MSSqlServer/README.md) |
| **PostgreSQL** 🐘 | `CSharpEssentials.LoggerHelper.Sink.Postgresql` | [📖 Read Docs](./src/CSharpEssentials.LoggerHelper.Sink.Postgresql/README.md) |
| **Elasticsearch** 🔍 | `CSharpEssentials.LoggerHelper.Sink.Elasticsearch` | [📖 Read Docs](./src/CSharpEssentials.LoggerHelper.Sink.Elasticsearch/README.md) |
| **Seq** 📈 | `CSharpEssentials.LoggerHelper.Sink.Seq` | [📖 Read Docs](./src/CSharpEssentials.LoggerHelper.Sink.Seq/README.md) |
| **Telegram** ✈️ | `CSharpEssentials.LoggerHelper.Sink.Telegram` | [📖 Read Docs](./src/CSharpEssentials.LoggerHelper.Sink.Telegram/README.md) |
| **Hangfire Console** ⏳| `CSharpEssentials.LoggerHelper.Sink.HangfireConsole` | [📖 Read Docs](./src/CSharpEssentials.LoggerHelper.Sink.HangfireConsole/README.md) |

---

## 🧩 Advanced Extensions

Supercharge your logging architecture with our official add-ons:

| Extension | NuGet Package | Description |
| :--- | :--- | :--- |
| **Dashboard** 📊 | `CSharpEssentials.LoggerHelper.Dashboard` | Add a real-time web UI to monitor live log streams, metrics, and manage sink states. |
| **MCP Tools** 🤖 | `CSharpEssentials.LoggerHelper.MCP` | Empower your local AI assistants to search, filter, and diagnose your application logs instantly. |

---

## 🛡️ Core Capabilities Overview

### Sensitive Data Masking
Ensure compliance by adding the masking configuration to your core settings. It intercepts and obfuscates patterns before they reach *any* sink.
```json
{
  "LoggerHelper": {
    "Masking": {
      "Enabled": true,
      "MaskString": "***MASKED***",
      "PropertiesToMask": ["Password", "CreditCard", "BearerToken"]
    }
  }
}
```

### Advanced Routing
Send `Warning` and `Error` logs to your Database, but keep everything on the Console for debugging:
```json
{
  "LoggerHelper": {
    "Routes": [
      {
        "Name": "Console",
        "MinimumLevel": "Debug"
      },
      {
        "Name": "MSSqlServer",
        "MinimumLevel": "Warning"
      }
    ]
  }
}
```

---
*Built with ❤️ by [YourName/Org] to make .NET logging effortless and scalable.*
