## 📘 Introduction<a id='introduction'></a>   [🔝](#table-of-contents)

🚀 **CSharpEssentials.LoggerHelper** is a flexible and modular structured logging library for .NET 6/8/9. It’s powered by Serilog for most sinks and extended with native support for others like Telegram (via `HttpClient`) and Email (via `System.Net`).
🧩 Each sink (File, PostgreSQL, Telegram, Email, etc.) is delivered as an independent NuGet sub-package and dynamically loaded at runtime.
📦 Centralized and intuitive configuration via a single `appsettings.LoggerHelper.json` file with built-in placeholder validation.
🪪 Supports rich structured logs with properties like `IdTransaction`, `ApplicationName`, `MachineName`, and `Action`.
🧠 Automatically captures the latest internal error (`CurrentError`), which can be exposed via HTTP headers or other channels.
🔧 Designed for extensibility with plugin support, level-based sink routing, Serilog SelfLog integration, and a safe debug mode.

👉 [Click here to view full usage guide and examples](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper/doc.md)

