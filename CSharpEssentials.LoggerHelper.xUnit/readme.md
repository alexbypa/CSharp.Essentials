# CSharpEssentials.LoggerHelper.xUnit

The **xUnit sink** for LoggerHelper allows you to capture log output directly inside your unit tests.  
This is especially useful when running tests in CI/CD pipelines or isolated environments where external sinks (DB, file, endpoints) may not be available.  

## Features
- Writes log entries into the xUnit output stream.  
- Respects log levels and filters defined in `appsettings.LoggerHelper.json`.  
- Helps debug flaky or disconnected tests without needing external infrastructure.  

## Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper.xUnit
