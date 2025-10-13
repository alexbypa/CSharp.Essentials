# 🖥️ CSharpEssentials.LoggerHelper.Dashboard

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Dashboard.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Dashboard)
An **embedded dashboard** for [CSharpEssentials.LoggerHelper](https://github.com/alexbypa/CSharp.Essentials), giving you **real-time visibility** into how sinks are loaded, which log levels are enabled, and any initialization errors — all from inside your application.

---
## 🚨 Critical Notice: Console Page Availability

The **Console Page** is available **only starting from version 4.1.0** of `CSharpEssentials.LoggerHelper.Dashboard`.

If you are using any version **below 4.1.0**, the **Console** section **will not appear** due to a **publishing error in earlier builds**.

### ✅ Required Action
Upgrade immediately to version **4.1.0 or higher**:


### 🎉 What's New 
With **version 4.0.7**, we’ve taken the Dashboard to a whole new level.  
A brand-new **Console Page** has been added, allowing you to **see in real time what’s happening inside your application** — directly within the dashboard!  

This feature mirrors the **Console Sink output** inside the web interface, making it incredibly easy to monitor your app’s internal behavior without external tools.  
And because production environments often restrict console access, this update ensures you **never miss critical logs**, even when your app runs remotely.

To protect sensitive data, **the Dashboard now supports optional Basic Authentication**.  
You can safely expose the dashboard behind authorized access, giving visibility only to trusted users — a must-have for production-grade observability.

> 🧠 **In short:** version 4.0.7 introduces the **Console Page** and **secured Basic Authentication**, turning your LoggerHelper Dashboard into a complete real-time monitoring hub.

With version **4.0.6**, we've introduced the highly requested ability to **customize the main Dashboard page**.

With version **4.0.5**, we've significantly simplified the integration and usage of the **AI-powered logging features**.

**[📝 View the Complete Changelog History Here](https://github.com/alexbypa/CSharp.Essentials/blob/main/CSharpEssentials.LoggerHelper.Dashboard/changelog.md)**

In release 4.0.5 introduces an easy-to-use factory pattern and extension methods to integrate the separate, powerful **`CSharpEssentials.LoggerHelper.AI`** package. This package enables advanced capabilities like **Vector Store (RAG) for logs**, **Anomaly Detection**, and **Trace Correlation** directly within your application's logging pipeline.

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