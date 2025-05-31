## Introduction

The **Console Sink** for **CSharpEssentials.LoggerHelper** lets you output structured Serilog events directly to the console (standard output), with support for colored output and JSON/text formatting.  
It plugs into the LoggerHelper **HUB** core via the `ISinkPlugin` mechanism, so you can route console-based logging based on your HUB configuration (e.g. per-level routing).

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
