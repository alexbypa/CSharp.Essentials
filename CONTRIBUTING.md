# Contributing to CSharp.Essentials

First off, thank you for considering contributing to **CSharp.Essentials**! 🎉

Whether you are reporting a bug, proposing a new feature, writing documentation, or implementing a new Sink Plugin, your help is warmly welcomed.

---

## 🧭 Ways to Contribute

1. **⭐ Star the repository**: An easy and impactful way to help the project gain visibility on GitHub!
2. **🐛 Report Bugs**: Open an issue if something doesn't work as expected.
3. **💡 Propose Features or Improvements**: Share ideas in GitHub issues or discussions.
4. **🔌 Build a New Sink Plugin**: Our architecture is 100% modular. Implementing a new sink (e.g., Discord, Slack, Grafana Loki, Sentry) is simple and isolated.
5. **📖 Improve Documentation & Samples**: Fix typos, add examples, or clarify guides.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8, 9, or 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/) (optional, only needed if testing external database sinks like Seq, Elasticsearch, PostgreSQL, SQL Server)
- Git

### Clone and Build

```bash
git clone https://github.com/alexbypa/CSharp.Essentials.git
cd CSharp.Essentials
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running the Demo Playground locally

```bash
cd src/CSharpEssentials.LoggerHelper.Demo/docker
cp .env.example .env
docker compose up -d

cd ..
dotnet run
```
Now navigate to `http://localhost:5123/playground.html` in your browser.

---

## 🔌 How to Build a Custom Sink Plugin in 15 Minutes

The `LoggerHelper` core is completely decoupled from individual sinks. Each sink is a separate project that implements `ISinkPlugin` and self-registers at startup using .NET's `[ModuleInitializer]`.

### Step 1: Create a Project
Create `CSharpEssentials.LoggerHelper.Sink.<TargetName>` referencing `CSharpEssentials.LoggerHelper`.

### Step 2: Implement `ISinkPlugin`

```csharp
using CSharpEssentials.LoggerHelper.Abstractions;
using Serilog;

namespace CSharpEssentials.LoggerHelper.Sink.MySink;

[LoggerHelperSink]
public sealed class MySinkPlugin : ISinkPlugin
{
    public bool CanHandle(string sinkName) =>
        sinkName.Equals("MySink", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options)
    {
        var opts = options.GetSinkConfig<MySinkOptions>("MySink")
                   ?? options.BindSinkSection<MySinkOptions>("MySink");

        if (opts is null || string.IsNullOrWhiteSpace(opts.WebhookUrl))
        {
            throw new InvalidOperationException("MySink requires a valid WebhookUrl.");
        }

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.YourSerilogSink(opts.WebhookUrl)
        );
    }
}
```

### Step 3: Register via `[ModuleInitializer]`

```csharp
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.MySink;

public static class MySinkInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        SinkPluginRegistry.Register(new MySinkPlugin());
    }
}
```

### Step 4: Add Unit Tests
Take inspiration from [CSharpEssentials.LoggerHelper.Sink.Telegram](file:///src/CSharpEssentials.LoggerHelper.Sink.Telegram) or [CSharpEssentials.LoggerHelper.Sink.File](file:///src/CSharpEssentials.LoggerHelper.Sink.File).

---

## 📋 Pull Request Guidelines

1. **Fork the repo** and create a descriptive branch: `feature/sink-discord` or `fix/http-timeout`.
2. **Follow existing coding conventions**: C# modern idioms, nullable reference types enabled.
3. **Ensure tests pass**: `dotnet test` must succeed with zero failures.
4. **Keep PRs focused**: One feature or bug fix per PR.
5. **Open a PR**: Link any related issues and provide a brief description of what was changed.

Thank you for helping make CSharp.Essentials better for everyone! ❤️
