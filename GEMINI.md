# CSharp.Essentials

## Project Overview

**CSharp.Essentials** is a modular NuGet package ecosystem providing infrastructure and utility libraries for .NET applications. The core offering is a centralized Serilog-based logging hub (`LoggerHelper`) with pluggable sinks, plus companion libraries for HTTP, encryption, serialization, reCAPTCHA, and HangFire.

- **Author:** Alessandro Chiodo
- **Architecture:** Modular library (per-package isolation, shared conventions)
- **Repository:** https://github.com/alexbypa/CSharp.Essentials
- **License:** MIT

## Obiettivo Principale

**Target:** Raggiungere almeno 1000 download giornalieri di `CSharpEssentials.LoggerHelper` su nuget.org entro 2 mesi, posizionandolo come una delle principali soluzioni di logging nella community .NET.
**Code base** Devono essere rispettati i principi SOLID, Clean Code, e le best practice di .NET. Il codice deve essere modulare, testabile, e facilmente estendibile con nuovi sink o funzionalità.
**Focus:** Solo `LoggerHelper` + sink + HttpHelper. Le altre librerie sono fuori scope per ora.
Tutte le modifiche le apporteremo sulla cartella src ( quindi prendiamo quello che c'è di buono sui progetti originali : CSharpEssentials.LoggerHelper e CSharpEssentials.LoggerHelper.Sink.... ) 

### Piano di Esecuzione

#### Fase 0 — Analisi Competitiva
Prima del rewrite, analizzare i top 10 pacchetti di logging su NuGet (download, API design, features, punti deboli). Identificare il gap nel mercato e posizionare LoggerHelper su quel gap come differenziatore.

#### Fase 1 — Rewrite del Core LoggerHelper
Creare un progetto LoggerHelper nuovo prendendo il meglio dall'originale, con queste migliorie:
- **JSON-first con Fluent minimale** — Il differenziatore e' la config JSON declarativa "zero code" (installi il NuGet, aggiungi JSON, funziona). L'API fluent (`AddRoute`, `ConfigureSink<T>`) esiste come complemento, non come feature primaria. Non hard-codare nomi di sink nel core per rispettare OCP
- **Compatibilita `ILogger<T>`** — Integrazione nativa con `Microsoft.Extensions.Logging` come provider. Chi gia usa `ILogger<T>` adotta LoggerHelper senza cambiare codice
- **Source Generator** — Sostituire il runtime reflection per il caricamento sink con source generator: piu veloce, AOT-compatible, trimming-safe. Differenziatore forte vs concorrenza
- **Benchmark pubblicati** — Comparazioni misurate vs Serilog puro, NLog, etc. con BenchmarkDotNet

#### Fase 2 — Rewrite dei Sink
Rifare i sink seguendo la nuova architettura del core, mantenendo la modularita per-package.

#### Fase 3 — Sito Frontend Promozionale
Creare un sito accattivante con:
- **Playground interattivo** — Mini-editor online dove provare la configurazione JSON e vedere i log in tempo reale ("Try LoggerHelper")
- **"Copy-paste ready" snippets** — Per ogni sink, uno snippet di 5 righe che funziona subito
- **Integration guides** — Guide specifiche per scenari comuni: "LoggerHelper + Minimal API", "LoggerHelper + Blazor", "LoggerHelper + Azure App Service", "LoggerHelper + Docker"
- Demo dal vivo, documentazione completa, getting started in 30 secondi

#### Fase 4 — Marketing e SEO
- **SEO NuGet** — Ottimizzare Description e Tags dei pacchetti con keyword piu cercate: "structured logging", "log routing", "multi-sink logging", "log level routing"
- **`dotnet new` template** — `dotnet new loggerhelper-api` che scaffolda un progetto pre-configurato. Friction di adozione a zero
- **Content marketing** — Blog post su dev.to/Medium, video YouTube, sample project su GitHub
- **Community** — Risposte su Stack Overflow, post su Reddit r/dotnet, engagement su Discord
- **GitHub Actions badge + stats widget** sul sito per social proof
- **README NuGet** ottimizzato con quick-start copy-paste e risultati benchmark

## Solution Structure

```si
CSharpEssentials.sln
|
|-- Core Libraries
|   |-- CSharpEssentials.LoggerHelper          (Serilog hub, plugin registry, middleware)
|   |-- CSharpEssentials.HttpHelper            (HttpClient + Polly resilience)
|   |-- CSharpEssentials.EncryptHelper         (Encryption utilities)
|   |-- CSharpEssentials.SerializerHelper      (Serialization helpers)
|   |-- CSharpEssentials.RecaptchaHelper       (reCAPTCHA integration)
|   |-- CSharpEssentials.HangFireHelper        (HangFire utilities)
|   |-- CSharpEssentials.HttpContextHelper     (HttpContext helpers)
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
|
|-- Extensions
|   |-- CSharpEssentials.LoggerHelper.Telemetry    (OpenTelemetry integration)
|   |-- CSharpEssentials.LoggerHelper.Dashboard    (Interactive log dashboard)
|   |-- CSharpEssentials.LoggerHelper.AI           (NL queries, anomaly detection)
|   |-- CSharpEssentials.LoggerHelper.xUnit        (Test output sink)
|
|-- Demo / Test
|   |-- LoggerHelperDemo/                          (Demo web app)
|   |-- LoggerHelperDemo.Tests/                    (xUnit integration tests)
|   |-- Test6.0/, Test8.0/                         (Framework-specific test apps)
|   |-- CSharpEssentials.TestWebApi/               (Test web API)
```

## Target Frameworks

Each package defines its own target framework(s). There is no global `Directory.Build.props`:

| Package | Targets |
|---|---|
| LoggerHelper (core) | net6.0, net8.0, net9.0, net10.0 |
| Most Sink plugins | net6.0, net8.0, net9.0 |
| HttpHelper | net8.0, net9.0 |
| EncryptHelper | net8.0 |
| LoggerHelper.xUnit | net8.0, net9.0 |

Do NOT change target frameworks without explicit request.

## Build Commands

```bash
# Build entire solution
dotnet build CSharpEssentials.sln

# Build a specific project
dotnet build CSharpEssentials.LoggerHelper/CSharpEssentials.LoggerHelper.csproj

# Pack a specific project
dotnet pack CSharpEssentials.LoggerHelper/CSharpEssentials.LoggerHelper.csproj -c Release

# Run tests
dotnet test LoggerHelperDemo.Tests/LoggerHelperDemo.Tests.csproj
```

## NuGet Packaging

- All library projects have `GeneratePackageOnBuild=True`
- Local package output paths vary per project (e.g., `D:\Nuget`, `D:\github\alexbypa\FlowScheduler\LocalNuget`)
- CI publishes the core LoggerHelper to nuget.org via GitHub Actions (`.github/workflows/publish.yml`)
- Each package manages its own `Version` in its `.csproj`

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

- **Framework:** xUnit v2 with Moq
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
