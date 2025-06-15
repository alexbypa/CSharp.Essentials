![Frameworks](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0-blue)
![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)
![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.svg)
![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.LoggerHelper.svg)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)
![GitHub Discussions](https://img.shields.io/github/discussions/alexbypa/CSharp.Essentials)
![Issues](https://img.shields.io/github/issues/alexbypa/CSharp.Essentials)

# 📦 CSharpEssentials.LoggerHelper

**The ultimate Serilog sink hub — extensible, modular, centralized.**

---

## 💡 Why CSharpEssentials.LoggerHelper?

**CSharpEssentials.LoggerHelper** is not just another logging library — it’s a **smart hub** for Serilog sinks.  
Thanks to its **modular architecture**, you can plug in only the sinks you need.

🔥 But there's more:  
For **Telegram** and **Email**, we bypass Serilog's limitations with native implementations via `HttpClient` and `System.Net.Mail`, unlocking **advanced formatting** and a built-in **ThrottleInterval** to prevent flooding your channels.

🔧 Configuration is centralized in a single file (`appsettings.LoggerHelper.json`), giving you full control over log levels and sink selection — no code changes required!

🧠 Error Insight Built-In:  
When something goes wrong, you can inspect:
- `CurrentError`: the last exception message
- `Errors`: the complete in-memory queue of failures  
Perfect for debugging deployment or configuration issues.
---

## 🆕 What’s New in 3.1.5?

### ✨ Added Sink: xUnit

When running tests in environments like DevOps pipelines, DBs and external endpoints may not always be accessible.  
No worries — with the **xUnit sink**, you get a full trace of failed tests directly inside your test output.  
Just install the package and define your desired levels (`Information`, `Warning`, `Error`, etc.).

> Perfect for debugging flaky tests or disconnected environments.

---

## 🔌 Available Sink Packages

- **Console**: `CSharpEssentials.LoggerHelper.Sink.Console`  
- **File**: `CSharpEssentials.LoggerHelper.Sink.File`  
- **MSSqlServer**: `CSharpEssentials.LoggerHelper.Sink.MSSqlServer`  
- **PostgreSQL**: `CSharpEssentials.LoggerHelper.Sink.PostgreSql`  
- **ElasticSearch**: `CSharpEssentials.LoggerHelper.Sink.Elasticsearch`  
- **Telegram**: `CSharpEssentials.LoggerHelper.Sink.Telegram` _Used via `HttpClient`_  
- **Email**: `CSharpEssentials.LoggerHelper.Sink.Email`_Used via `System.Net.Mail`_  
- **xUnit**: `CSharpEssentials.LoggerHelper.Sink.xUnit` ✅ ( new from 3.1.5 )

---

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)

