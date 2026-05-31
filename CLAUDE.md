# CSharp.Essentials

## Project Overview

**CSharp.Essentials** is a modular NuGet package ecosystem providing infrastructure and utility libraries for .NET applications. 
The core offering is a centralized Serilog-based logging hub (`LoggerHelper`) with pluggable sinks, plus companion libraries for HTTP, encryption, serialization, reCAPTCHA, and HangFire.
**Documetation"" La documentazione si trova sulla cartella D:\Project_Pixelo\CSharp.Essentials\docs\site e in remoto su 

- **Author:** Alessandro Chiodo
- **Architecture:** Modular library (per-package isolation, shared conventions)
- **Repository:** https://github.com/alexbypa/CSharp.Essentials
- **License:** MIT

## Obiettivo Principale
**Target:** Raggiungere almeno 1000 download giornalieri di `CSharpEssentials.LoggerHelper` su nuget.org entro 2 mesi, posizionandolo come una delle principali soluzioni di logging nella community .NET.
**Code base** Devono essere rispettati i principi SOLID, Clean Code, e le best practice di .NET. Il codice deve essere modulare, testabile, e facilmente estendibile con nuovi sink o funzionalità.
**Memory leak"" Zero problemi di memory leak 

### Piano di Esecuzione

#### Fase 4 — Marketing e SEO
- **SEO NuGet** — Ottimizzare Description e Tags dei pacchetti con keyword piu cercate: "structured logging", "log routing", "multi-sink logging", "log level routing"
- **`dotnet new` template** — `dotnet new loggerhelper-api` che scaffolda un progetto pre-configurato. Friction di adozione a zero
- **Content marketing** — Blog post su dev.to/Medium, video YouTube, sample project su GitHub
- **Community** — Risposte su Stack Overflow, post su Reddit r/dotnet, engagement su Discord
- **GitHub Actions badge + stats widget** sul sito per social proof
- **README NuGet** ottimizzato con quick-start copy-paste e risultati benchmark

## Solution Structure

```markdown
CSharpEssentials.sln
|
|-- Core Libraries
|   |-- CSharpEssentials.LoggerHelper          (Serilog hub, plugin registry, middleware)
|
|-- Sink Plugins (each a separate NuGet)
|   |-- CSharpEssentials.LoggerHelper.Sink.Console
|   |-- CSharpEssentials.LoggerHelper.Sink.File
|   |-- CSharpEssentials.LoggerHelper.Sink.Email
|   |-- CSharpEssentials.LoggerHelper.Sink.Telegram
|   |-- CSharpEssentials.LoggerHelper.Sink.Elasticsearch
|   |-- CSharpEssentials.LoggerHelper.Sink.MSSqlServer
|   |-- CSharpEssentials.LoggerHelper.Sink.Postgresql
|   |-- CSharpEssentials.LoggerHelper.Sink.Seq
|   |-- CSharpEssentials.LoggerHelper.Sink.CSharpEssentials.LoggerHelper.Sink.HangfireConsole
|

## NuGet Packaging

- All library projects have `GeneratePackageOnBuild=True`
- Local package output paths vary per project `C:\Nuget`
- CI publishes the core LoggerHelper to nuget.org via GitHub Actions (`.github/workflows/publish.yml`)
- Each package manages its own `Version` in its `.csproj`
- Check at any change on code readme.md on https://github.com/alexbypa/CSharp.Essentials/blob/main/README.md
- Check at any change on code documentazion on https://www.loggerhelper.com/

## Key Architecture Patterns

### Sink Plugin System
The core `LoggerHelper` uses a plugin architecture for sinks:
- **`ISinkPlugin`** — interface each sink must implement (`CanHandle`, `HandleSink`)
- **`SinkPluginRegistry`** — global static registry where plugins self-register
- Sinks are loaded dynamically at runtime via `TolerantPluginLoadContext`
- Configuration is JSON-based (`SerilogConfiguration`, `SerilogCondition`) allowing per-level sink routing

### Core Interfaces
- **`ILoggerRequest`** / **`IRequest`** — standard structured fields (IdTransaction, Action, ApplicationName)
- **`IContextLogEnricher`** — custom log enrichment
- **`RequestResponseLoggingMiddleware`** — ASP.NET Core middleware for request/response logging

### Configuration Model
- `SerilogConfiguration` — root config with `ApplicationName`, `SerilogCondition[]`, `SerilogOption`
- `SerilogCondition` — maps a sink name to log levels
- `SerilogOption` — per-sink configuration (MSSqlServer, PostgreSQL, Telegram, Email, File, Elasticsearch, Seq)

## Coding Conventions

- **Namespace pattern:** `CSharpEssentials.{PackageName}` (e.g., `CSharpEssentials.LoggerHelper`)
- **File-scoped namespaces** used throughout
- **Nullable:** enabled in all projects
- **ImplicitUsings:** enabled in all projects
- **Brace style:** opening brace on same line as declaration (K&R style)
- **Comments:** mix of Italian and English — follow the existing language in each file
- **XML docs:** present on public APIs (interfaces, key classes)
- **No Directory.Build.props** — each `.csproj` is self-contained

## Testing

- **Framework:** .Net 10.0 xUnit v2 with Moq
- **Test project:** `LoggerHelperDemo.Tests` (integration tests against LoggerHelperDemo)
- **Additional:** `CSharpEssentials.LoggerHelper.xUnit` is a *sink package* (not a test project) that forwards logs to xUnit test output
- **Coverage:** coverlet.collector
- **Pattern:** Arrange-Act-Assert with `HttpMessageHandler` mocking for HTTP tests

## CI/CD

- **GitHub Actions** workflow at `.github/workflows/publish.yml`
- Triggers on push to `main`
- Currently builds and publishes only `CSharpEssentials.LoggerHelper` to nuget.org
- Uses `NUGET_API_KEY` secret

## Dependencies to Know

| Dependency | Used In |
|---|---|
| Serilog | Core + all sinks |
| Polly | HttpHelper (resilience) |
| Moq | HttpHelper, Tests |
| Microsoft.AspNetCore.Mvc.Testing | LoggerHelperDemo.Tests |
| System.Diagnostics.DiagnosticSource | Core (OpenTelemetry bridge) |

## Guidelines for New Sink Plugins

When creating a new sink plugin:
1. Create project `CSharpEssentials.LoggerHelper.Sink.{Name}`
2. Implement `ISinkPlugin` (CanHandle + HandleSink)
3. Register via `SinkPluginRegistry.Register(...)` in a module initializer or static constructor
4. Reference `CSharpEssentials.LoggerHelper` as a **NuGet package** (not project reference)
5. Add configuration class to `SerilogOption` if needed
6. Set `GeneratePackageOnBuild=True`, add package metadata (icon, readme, license)
7. Multi-target to match existing sink conventions (net6.0, net8.0, net9.0)