# CSharp.Essentials

## Project Overview

**CSharp.Essentials** is a modular NuGet package ecosystem providing infrastructure and utility libraries for .NET applications. 
The core offering is a centralized Serilog-based logging hub (`LoggerHelper`) with pluggable sinks, plus a companion library for HTTP resilience (`HttpHelper`).

- **Documentation:** Local at `D:\Project_Pixelo\CSharp.Essentials\docs\site`, live at https://www.loggerhelper.com/
- **Author:** Alessandro Chiodo
- **Architecture:** Modular library (per-package isolation, shared conventions)
- **Repository:** https://github.com/alexbypa/CSharp.Essentials
- **License:** MIT
- **Current Version:** 5.0.1 (unified across all 11 packages)
- **PackageProjectUrl:** https://www.loggerhelper.com (all packages)

## Obiettivo Principale

- **Target:** Raggiungere almeno 1000 download giornalieri di `CSharpEssentials.LoggerHelper` su nuget.org entro 2 mesi, posizionandolo come una delle principali soluzioni di logging nella community .NET.
- **Code base:** Devono essere rispettati i principi SOLID, Clean Code, e le best practice di .NET. Il codice deve essere modulare, testabile, e facilmente estendibile con nuovi sink o funzionalita.
- **Memory leak:** Zero problemi di memory leak.

### Piano di Esecuzione

#### Fase 4 — Marketing e SEO
- ~~**SEO NuGet** — Ottimizzare Description e Tags dei pacchetti~~ (COMPLETATO v5.0.1)
- **`dotnet new` template** — `dotnet new loggerhelper-api` che scaffolda un progetto pre-configurato. Friction di adozione a zero
- **Content marketing** — Blog post su dev.to/Medium, video YouTube, sample project su GitHub
- **Community** — Risposte su Stack Overflow, post su Reddit r/dotnet, engagement su Discord
- **GitHub Actions badge + stats widget** sul sito per social proof
- ~~**README NuGet** ottimizzato con quick-start copy-paste~~ (COMPLETATO v5.0.1 — ogni pacchetto ha README dedicato)

## Solution Structure

```markdown
CSharpEssentials.LoggerHelper.slnx
|
|-- Core Libraries
|   |-- CSharpEssentials.LoggerHelper          (Serilog hub, plugin registry, middleware)
|   |-- CSharpEssentials.HttpHelper            (HttpClient + Polly resilience)
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
|   |-- CSharpEssentials.LoggerHelper.Sink.HangfireConsole
```

## NuGet Packaging

- All library projects have `GeneratePackageOnBuild=True`
- Local package output path: `C:\Nuget`
- CI publishes **all 11 packages** (core + 9 sinks + HttpHelper) to nuget.org via GitHub Actions (`.github/workflows/publish.yml`)
- Each package manages its own `Version` in its `.csproj` (currently all at 5.0.1)
- Icon path: `..\..\img\CSharpEssentials.png` (relative from each project)
- Check at any change on code: update README.md on https://github.com/alexbypa/CSharp.Essentials/blob/main/README.md
- Check at any change on code: update documentation on https://www.loggerhelper.com/
- When user approves code changes, provide a commit message for git.
 
## Key Architecture Patterns

### Sink Plugin System
The core `LoggerHelper` uses a plugin architecture for sinks:
- **`ISinkPlugin`** — interface each sink must implement (`CanHandle` + `Configure`)
- **`[LoggerHelperSink]`** attribute + `[ModuleInitializer]` for auto-registration
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
- Config chain: `GetSinkConfig<T>()` (fluent) → `BindSinkSection<T>()` (JSON) → `new T()` (defaults)

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
- Triggers on push to `main` + `workflow_dispatch` (manual run from GitHub UI)
- Builds entire solution, packs and publishes **all 11 packages** to nuget.org
- Uses `NUGET_API_KEY` secret (glob scope: `CSharpEssentials.*`)

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
2. Implement `ISinkPlugin` (`CanHandle` + `Configure`)
3. Add `[LoggerHelperSink]` attribute and register via `[ModuleInitializer]`
4. Reference `CSharpEssentials.LoggerHelper` as a **project reference**
5. Add configuration class to `SerilogOption` if needed
6. Set `GeneratePackageOnBuild=True`, add package metadata (icon at `..\..\img\CSharpEssentials.png`, readme, license)
7. Multi-target: `net6.0;net8.0;net9.0;net10.0`


## TODO: 
 Step 4 — Review documentazione (Obiettivo 3)

 Analisi della documentazione di CSharpEssentials.LoggerHelper.Sink.HangfireConsole:

 README GitHub — Punti di forza:

 - Installazione chiara (2 pacchetti NuGet)
 - Due opzioni di config (JSON + Fluent API)
 - Pattern Set/Clear del PerformContext spiegato
 - Color mapping documentato

 README GitHub — Punti deboli / Suggerimenti miglioramento:

 1. Manca esempio completo end-to-end — Il README mostra frammenti ma non un Program.cs completo con Host.CreateDefaultBuilder, DI, e Hangfire setup
 2. Manca sezione Troubleshooting — Cosa succede se non chiami accessor.Clear()? Memory leak? Log che finiscono nel job sbagliato?
 3. Manca prerequisito Hangfire.Console — Non dice esplicitamente che serve .UseConsole() nella configurazione Hangfire
 4. Versione minima .NET non chiara — Supporta net6? net8? net10?

 loggerhelper.com — Punti di forza:

 - Overview chiara di tutti i 9 sink
 - Tabella comparativa
 - Sia Fluent API che JSON config

 loggerhelper.com — Punti deboli:

 1. HangfireConsole ha solo 3 righe — Rispetto agli altri sink è sottodocumentato
 2. Manca menzione della compatibilità con il formato legacy Serilog:SerilogConfiguration nel sito (è solo nel README)