## Introduction

The **MSSQL Server Sink** for **CSharpEssentials.LoggerHelper** lets you persist structured Serilog events into a Microsoft SQL Server database table.  
It plugs into the LoggerHelper **HUB** core via the `ISinkPlugin` mechanism, so you can route log events to SQL Server based on your HUB configuration (e.g. per-level routing).

---

## Key Features

- 🗄️ **Auto-create table**: optionally create your log table if it doesn’t exist  
- 🔢 **Custom column schema**: include or exclude columns, rename columns, use custom mappings  
- 🔀 **Level-based routing**: send Error, Warning, Information, etc. into different tables or schemas  
- 🔧 **Batching & performance**: configure batch size, period and retention options  
- 🔐 **Secure connection**: support for encrypted connection strings and integrated authentication  

---

## 🚀 Basic Usage

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)
