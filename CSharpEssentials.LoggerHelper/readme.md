# 📦 CSharpEssentials.LoggerHelper

## Introduction

**CSharpEssentials.LoggerHelper** is a flexible, modular structured logging **HUB** for .NET (6.0/8.0) applications built on Serilog.  
The **Hub core** package acts as a central routing engine—directing log events to one or more sinks based on your configuration and log level.  
All built-in sink implementations have been removed from the core and moved into dedicated sub-packages, so you install only the sinks you need and can extend the Hub with **any number** of additional sinks.

---

## Key Benefits

- 🔧 **HUB Core**: minimal dependencies, central logger routing engine  
- 🟢 **Structured logs**: includes Action, IdTransaction, ApplicationName, MachineName  
- 🔀 **Level-based routing**: assign sinks per log level (Information, Warning, Error, …)  
- 📦 **Modular sinks**: each sink lives in its own NuGet package under `CSharpEssentials.LoggerHelper.Sink.*`  
- ➕ **Infinite extensibility**: add as many sinks as you want by installing extra sub-packages  
- ⚡️ **Placeholder validation**: catches template mismatches at startup  
- 📁 **Single config file**: `appsettings.LoggerHelper.json`

---

## Available Sink Packages

- **Console**: `CSharpEssentials.LoggerHelper.Sink.Console`  
- **File**: `CSharpEssentials.LoggerHelper.Sink.File`  
- **MSSqlServer**: `CSharpEssentials.LoggerHelper.Sink.MSSqlServer`  
- **PostgreSQL**: `CSharpEssentials.LoggerHelper.Sink.PostgreSql`  
- **ElasticSearch**: `CSharpEssentials.LoggerHelper.Sink.Elasticsearch`  
- _…and any custom sink you implement_

---

## 🚀 Basic Usage

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)