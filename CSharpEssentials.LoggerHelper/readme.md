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

**Key strengths include:**

- **Centralized & flexible configuration** – Choose which sinks receive which log levels using `appsettings.LoggerHelper.json`. For example, send only `Error` messages to Email while routing all logs to ElasticSearch. No code changes required.
- *- **Unified observability & dashboard** – The `Sink.Telemetry` package integrates logs, metrics and traces via OpenTelemetry. Each log entry carries a `trace_id` so you can correlate distributed requests. An interactive dashboard lets you visualize traces, sink errors and telemetry, and configure alerts.

- **Structured properties & enrichment** – Standard fields like `IdTransaction`, `ApplicationName`, `MachineName` and `Action` are included by default. You can add custom properties (e.g., username, IP) with enricher functions, and they'll appear across all sinks.
- **Modular architecture & error inspection** – Each sink (Console, File, MSSQL, PostgreSQL, ElasticSearch, Email, Telegram, xUnit, Telemetry) is a separate NuGet package. Install only what you need; the core loads them dynamically. It also exposes `CurrentError` and an in-memory `Errors` queue to help debug initialization failures.

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
- - **"Telemetry"**: `CSharpEssentials.LoggerHelper.Sink.Telemetry` – `CSharpEssentials.LoggerHelper.Sink.Telemetry` collects logs, metrics and traces using OpenTelemetry.


---

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)

