# 📊 CSharpEssentials.LoggerHelper.Sink.Telemetry

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Telemetry.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Telemetry)
A full **OpenTelemetry sink** for [CSharpEssentials.LoggerHelper](https://github.com/alexbypa/CSharp.Essentials), enabling **metrics, traces, and logs** with automatic database storage for end-to-end observability.

---

## 🔥 Key Features

* 📊 Native integration with **OpenTelemetry**.
* 🐘💾 Automatic persistence of **metrics, traces, and logs** into SQL Server or PostgreSQL.
* ⚡ **Database auto-bootstrap**: tables are created automatically at startup (no migrations required).
* 🔗 Built-in correlation via `IdTransaction` between **logs** and **traces**.
* 🔧 Support for **custom metrics** (`GaugeWrapper`, `CustomMetrics`).

---

## 📦 Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Telemetry
```

---

## 🚀 Demo Project

A full demo with Telemetry, metrics, and traces is available in the [**CSharpEssentials.Extensions**](https://github.com/alexbypa/Csharp.Essentials.Extensions/tree/main) repository.

---
