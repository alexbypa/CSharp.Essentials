## Introduction

The **PostgreSQL Sink** for **CSharpEssentials.LoggerHelper** lets you persist structured Serilog events into a PostgreSQL table.  
It plugs into the LoggerHelper **HUB** core via the `ISinkPlugin` mechanism, so you can route log events to PostgreSQL based on your HUB configuration (e.g. per-level routing).

---

## Key Features

- 🗄️ **Auto-create table**: optionally create your log table if it doesn’t exist  
- 🌐 **JSON/JSONB support**: store event properties in a JSON(B) column for flexible querying  
- 🔀 **Level-based routing**: send Information, Warning, Error, etc. into different tables or schemas  
- 🔧 **Batching & performance**: configure batch posting limit and period for efficient inserts  
- 🔐 **Secure connection**: support for SSL and integrated authentication  

---

## 🚀 Basic Usage

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)
