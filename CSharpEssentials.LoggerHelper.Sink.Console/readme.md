## Introduction

The **Console Sink** for **CSharpEssentials.LoggerHelper** lets you output structured Serilog events directly to the console (standard output), with support for colored output and JSON/text formatting.  
It plugs into the LoggerHelper **HUB** core via the `ISinkPlugin` mechanism, so you can route console-based logging based on your HUB configuration (e.g. per-level routing).

---

### ⚡ Version 4.0.10 — Extended Console Integration

Starting from **version 4.0.10**, the **Console Sink** can now **forward console messages directly to the LoggerHelper Dashboard**.  
This enhancement allows developers to **visualize all console output inside the web dashboard** right at application startup — perfect for monitoring initialization steps and early configuration logs.

Additionally, this integration can be **extended and customized** within your own projects by leveraging the `TraceDashBoardSync` and `TraceDashBoardAsync` methods, giving you full control over how console logs are collected and displayed.

> 🧠 In short: version 4.0.10 connects the Console Sink to the Dashboard for real-time visibility at startup — and lets you extend it for your own logging scenarios.

---

## Key Features

- 🖥️ **Colorized output**: easily distinguish Information, Warning, Error, etc. by custom colors  
- 📜 **Text or JSON formatting**: choose the built-in console formatter or output raw JSON for each event  
- 🔀 **Level-based routing**: send only specific levels (e.g. Debug, Information, Error) to the console  
- 🧩 **Minimal configuration**: no external infrastructure needed—great for local development and diagnostics  
- ⚙️ **Flexible themes**: support for Serilog “ConsoleTheme” customization  

---

## 🚀 Basic Usage

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)
