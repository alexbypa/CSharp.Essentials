# 🖥️ CSharpEssentials.LoggerHelper.Dashboard

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Dashboard.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Dashboard)
An **embedded dashboard** for [CSharpEssentials.LoggerHelper](https://github.com/alexbypa/CSharp.Essentials), giving you **real-time visibility** into how sinks are loaded, which log levels are enabled, and any initialization errors — all from inside your application.

---

### 🎉 What's New in Version 4.0.5

With version **4.0.5**, we've significantly simplified the integration and usage of the **AI-powered logging features**.

This release introduces an easy-to-use factory pattern and extension methods to integrate the separate, powerful **`CSharpEssentials.LoggerHelper.AI`** package. This package enables advanced capabilities like **Vector Store (RAG) for logs**, **Anomaly Detection**, and **Trace Correlation** directly within your application's logging pipeline.

**Simplified Integration:**

* **AI Service Registration:** Seamlessly register the AI services using new extension methods, simplifying the setup in your `Program.cs`.
* **Flexible Persistence:** Easy configuration for both in-memory and **SQL-based Vector Store persistence**, allowing you to choose the right data layer for your AI logs.

For detailed instructions and examples, please refer to the dedicated documentation for the **[CSharpEssentials.LoggerHelper.AI package](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.AI)**.

---

## 🔥 Key Features

* 🖥️ Built-in **web dashboard** served directly by your ASP.NET Core app.
* 🌍 Accessible at the path `/ui` → e.g. if your app runs on `http://localhost:1234`, the dashboard is available at `http://localhost:1234/ui`.
* 📦 Lists all **registered sinks** with their **configured levels** (`Information`, `Warning`, `Error`, …).
* 🚨 Highlights **sink loading errors** so you can detect misconfigurations instantly.
* 📊 Extends visibility with **logs, traces, and metrics** in one UI.
* ⚡ No external dependencies — lightweight and production-ready.

---

## 📦 Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper.Dashboard
```

---

## 🚀 Demo Project
A full demo of the Dashboard, including sink loading details and level-based configuration, is available in the [**CSharpEssentials.Extensions**](https://github.com/alexbypa/Csharp.Essentials.Extensions) repository.
---