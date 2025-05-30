# CSharpEssentials.LoggerHelper.Sink.File


## Introduction

The **File Sink** for **CSharpEssentials.LoggerHelper** lets you write structured Serilog events to rolling or single log files.  
It plugs into the LoggerHelper **HUB** core via the `ISinkPlugin` mechanism, so you can route file-based logging based on your HUB configuration (e.g. per-level routing).

---

## Key Features

- 📝 **JSON or text formatting**: choose `JsonFormatter` or any Serilog formatter  
- 📂 **Rolling & retention**: configure rolling interval, file size limits, retained file count  
- 🔀 **Level-based routing**: send only Information, Warning, Error, etc. to file  
- 🔧 **Flexible paths**: custom directory, naming pattern, shared vs. exclusive access  

---

## 🚀 Basic Usage

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)
