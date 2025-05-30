## Introduction

The **Elasticsearch Sink** for **CSharpEssentials.LoggerHelper** lets you route structured Serilog events into an Elasticsearch cluster.  
It plugs into the LoggerHelper **HUB** core via the `ISinkPlugin` mechanism, so logs will be sent to Elasticsearch based on your HUB configuration (e.g. per-level routing).

---

## Key Features

- 🔍 **Auto-indexing**: writes each log event as a JSON document in your index  
- 📑 **Custom index templates**: control mappings, shard count, replicas  
- 🔀 **Level-based routing**: use the HUB core to send only Error-level (or other) events here  
- 🔧 **Full Serilog options**: buffer size, batch posting period, inline fields  

---

## 🚀 Basic Usage

👉 **Check out how to use the package in the documentation**  
📖 [View the usage guide here!](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)