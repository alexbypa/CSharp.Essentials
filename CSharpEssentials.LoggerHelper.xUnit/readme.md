# 🧪 CSharpEssentials.LoggerHelper.Sink.xUnit

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.xUnit.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.xUnit)
A lightweight **xUnit sink** for [CSharpEssentials.LoggerHelper](https://github.com/alexbypa/CSharp.Essentials), designed to make integration tests and CI/CD pipelines more **observable** by streaming logs directly into the xUnit output window.

---

## 🔥 Key Features

* 🧪 **Forward logs to xUnit output** for instant visibility during test runs.
* 🚀 Perfect for **integration tests** and **CI/CD pipelines** (e.g., Azure DevOps, GitHub Actions).
* ⚡ Works seamlessly with LoggerHelper’s **structured logging** and contextual properties (`IdTransaction`, `Action`, `ApplicationName`, …).
* 🔧 Simple configuration, no external services required.

---

## 📦 Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.xUnit
```

---

## ⚡ Quick Configuration

Add the sink to your `appsettings.LoggerHelper.json`:

```json
{
  "Serilog": {
    "SerilogConfiguration": {
      "SerilogCondition": [
        {
          "Sink": "xUnit",
          "Level": [ "Information", "Warning", "Error", "Fatal" ]
        }
      ]
    }
  }
}
```

In your test class, link the sink to xUnit’s output stream:

```csharp
public class MinimalEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MinimalEndpointTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;

        // 🚨 REQUIRED: link logger to xUnit output
        XUnitTestOutputHelperStore.SetOutput(output);
    }

    [Fact]
    public async Task Login_ShouldTimeout_WhenTokenInvalid()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/auth/check");
        response.EnsureSuccessStatusCode();
    }
}
```

---
