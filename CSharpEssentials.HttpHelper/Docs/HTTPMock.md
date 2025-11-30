# 🎭 HTTP Mock Engine Documentation

## 📋 Overview

The **HTTP Mock Engine** allows you to intercept and mock HTTP requests in your application using **Moq**. This is particularly useful for:

- ✅ **Testing** without hitting real APIs
- ✅ **Simulating timeouts, errors, and edge cases**
- ✅ **Sequential responses** (first call fails, second succeeds)
- ✅ **Conditional mocking** based on request properties

---

## 🚀 Quick Start

### 1️⃣ Installation

Already included in `CSharpEssentials.HttpHelper`!

### 2️⃣ Setup in `Program.cs`

```csharp
using CSharpEssentials.HttpHelper.HttpMocks;

var builder = WebApplication.CreateBuilder(args);

// Register your mock scenarios
builder.Services.AddTransient<IHttpMockScenario, HttpMockLibraryTimeoutThenOk>();
builder.Services.AddTransient<IHttpMockScenario, HttpMockLibraryAlwaysOk>();

// Register the mock engine
builder.Services.AddTransient<IHttpMockEngine, HttpMockEngine>();

// Register HTTP clients (mock engine is automatically injected)
builder.Services.AddHttpClients(builder.Configuration);

var app = builder.Build();
app.Run();
```

> ⚠️ **Important**: Use `AddTransient` for mock scenarios to ensure each request gets a fresh instance!

---

## 🎯 Core Concepts

### `IHttpMockScenario`

Defines **what requests to mock** and **how to respond**:

```csharp
public interface IHttpMockScenario {
    // Match condition: when should this mock activate?
    Func<HttpRequestMessage, bool> Match { get; }
    
    // Response sequence: what responses to return (in order)
    IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory { get; }
}
```

### `IHttpMockEngine`

Orchestrates all mock scenarios:

```csharp
public interface IHttpMockEngine {
    IEnumerable<IHttpMockScenario> scenarios { get; }
    
    // Check if ANY scenario matches the request
    bool Match(HttpRequestMessage request);
    
    // Build the Moq handler with all scenarios
    HttpMessageHandler Build();
}
```

---

## 📝 Creating Mock Scenarios

### Example 1: Simple Always-OK Mock

```csharp
public class HttpMockLibraryAlwaysOk : IHttpMockScenario {
    public Func<HttpRequestMessage, bool> Match => request =>
        request.RequestUri?.AbsoluteUri.Contains("library.com") == true;

    public IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory => new List<Func<Task<HttpResponseMessage>>> {
        () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"status\":\"success\",\"message\":\"All good!\"}")
        })
    };
}
```

**Registration:**
```csharp
builder.Services.AddTransient<IHttpMockScenario, HttpMockLibraryAlwaysOk>();
```

---

### Example 2: Sequential Responses (Timeout → OK)

```csharp
public class HttpMockLibraryTimeoutThenOk : IHttpMockScenario {
    public Func<HttpRequestMessage, bool> Match => request =>
        request.RequestUri?.AbsoluteUri.Contains("library.com/timeout-test") == true;

    public IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory => new List<Func<Task<HttpResponseMessage>>> {
        // 1st call: Simulate timeout
        async () => {
            await Task.Delay(5000); // 5 seconds delay
            throw new TaskCanceledException("Request timeout");
        },
        // 2nd call: Return success
        () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"status\":\"recovered\",\"message\":\"Now it works!\"}")
        }),
        // 3rd+ calls: Keep returning success
        () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"status\":\"still_ok\"}")
        })
    };
}
```

**Registration:**
```csharp
builder.Services.AddTransient<IHttpMockScenario, HttpMockLibraryTimeoutThenOk>();
```

---

### Example 3: Conditional Mock Based on Headers

```csharp
public class HttpMockWithAuthHeader : IHttpMockScenario {
    public Func<HttpRequestMessage, bool> Match => request => {
        if (request.RequestUri?.Host != "api.example.com") return false;
        
        // Only mock if Authorization header is missing
        return !request.Headers.Contains("Authorization");
    };

    public IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory => new List<Func<Task<HttpResponseMessage>>> {
        () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) {
            Content = new StringContent("{\"error\":\"Missing Authorization header\"}")
        })
    };
}
```

---

### Example 4: Mock Different HTTP Methods

```csharp
public class HttpMockPostOnly : IHttpMockScenario {
    public Func<HttpRequestMessage, bool> Match => request =>
        request.Method == HttpMethod.Post &&
        request.RequestUri?.AbsolutePath == "/api/users";

    public IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory => new List<Func<Task<HttpResponseMessage>>> {
        () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) {
            Content = new StringContent("{\"id\":123,\"created\":true}")
        })
    };
}
```

---

## 🔧 Advanced: Using `HttpMockScenario` DTO

For dynamic scenarios, use the `HttpMockScenario` class:

```csharp
var dynamicScenario = new HttpMockScenario(
    match: request => request.RequestUri?.Host == "dynamic.api",
    responseFactory: new List<Func<Task<HttpResponseMessage>>> {
        () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"dynamic\":true}")
        })
    }
);

builder.Services.AddTransient<IHttpMockScenario>(_ => dynamicScenario);
```

---

## 🎮 Usage in Your Application

Once registered, mocks are **automatically applied** when using `IhttpsClientHelperFactory`:

```csharp
app.MapGet("/test-mock", async (IhttpsClientHelperFactory httpFactory) => {
    var client = httpFactory.CreateClient();
    
    // This will be intercepted by HttpMockLibraryTimeoutThenOk
    var response1 = await client.GetAsync("https://library.com/timeout-test");
    Console.WriteLine($"1st call: {response1.StatusCode}"); // ⏱️ Timeout (throws)
    
    var response2 = await client.GetAsync("https://library.com/timeout-test");
    Console.WriteLine($"2nd call: {response2.StatusCode}"); // ✅ OK
    
    var response3 = await client.GetAsync("https://library.com/timeout-test");
    Console.WriteLine($"3rd call: {response3.StatusCode}"); // ✅ OK
    
    return Results.Ok(new { mock = "demonstrated" });
});
```

---

## 🧪 How It Works

### Request Flow with Mocks

```
HTTP Request
    ↓
HttpMockDelegatingHandler
    ↓
IHttpMockEngine.Match(request)
    ↓
┌─────────── Match = false ──────────┐
│                                    ↓
│                          Real HTTP Call
│                                    
└─────────── Match = true ───────────┐
                                     ↓
                        IHttpMockEngine.Build()
                                     ↓
                        Moq Handler (Strict)
                                     ↓
                        Find matching IHttpMockScenario
                                     ↓
                        Return ResponseFactory[sequence]
```

### Behind the Scenes

```csharp
public HttpMessageHandler Build() {
    var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    
    foreach (var scenario in scenarios) {
        var seq = mock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => scenario.Match(r)),
                ItExpr.IsAny<CancellationToken>());

        foreach (var responseFactory in scenario.ResponseFactory)
            seq.Returns(responseFactory);
    }
    
    return mock.Object;
}
```

**Key Points:**
- Uses `SetupSequence` for multiple responses
- `MockBehavior.Strict` ensures all calls are explicitly configured
- Fresh handler created per request (no caching issues)

---

## ⚠️ Common Pitfalls

### ❌ DON'T: Use `AddScoped` or `AddSingleton`

```csharp
// ❌ BAD: State is shared across requests!
builder.Services.AddScoped<IHttpMockScenario, HttpMockLibraryTimeoutThenOk>();
```

**Problem:** Sequential responses won't work correctly on subsequent requests.

### ✅ DO: Use `AddTransient`

```csharp
// ✅ GOOD: Fresh instance per request
builder.Services.AddTransient<IHttpMockScenario, HttpMockLibraryTimeoutThenOk>();
```

---

### ❌ DON'T: Forget to register `IHttpMockEngine`

```csharp
// ❌ BAD: Scenarios won't be used!
builder.Services.AddTransient<IHttpMockScenario, MyScenario>();
// Missing: AddTransient<IHttpMockEngine, HttpMockEngine>()
```

### ✅ DO: Register both scenarios AND engine

```csharp
// ✅ GOOD
builder.Services.AddTransient<IHttpMockScenario, MyScenario>();
builder.Services.AddTransient<IHttpMockEngine, HttpMockEngine>();
```

---

## 🔍 Debugging Mocks

### Check if Mock is Matching

```csharp
app.MapGet("/debug-mock", async (IHttpMockEngine engine, IhttpsClientHelperFactory factory) => {
    var request = new HttpRequestMessage(HttpMethod.Get, "https://library.com/test");
    
    bool isMatched = engine.Match(request);
    Console.WriteLine($"🎯 Mock matched: {isMatched}");
    
    var scenarios = engine.scenarios.Select(s => new {
        Type = s.GetType().Name,
        Matches = s.Match(request)
    });
    
    return Results.Ok(new { matched = isMatched, scenarios });
});
```

---

## 📚 Complete Example

### `Program.cs`

```csharp
using CSharpEssentials.HttpHelper;
using CSharpEssentials.HttpHelper.HttpMocks;

var builder = WebApplication.CreateBuilder(args);

// Register all mock scenarios
builder.Services.AddTransient<IHttpMockScenario, HttpMockLibraryTimeoutThenOk>();
builder.Services.AddTransient<IHttpMockScenario, HttpMockLibraryAlwaysOk>();
builder.Services.AddTransient<IHttpMockScenario, HttpMockUnauthorized>();

// Register mock engine
builder.Services.AddTransient<IHttpMockEngine, HttpMockEngine>();

// Register HTTP clients (automatically uses mock engine)
builder.Services.AddHttpClients(builder.Configuration);

var app = builder.Build();

app.MapGet("/demo", async (IhttpsClientHelperFactory httpFactory) => {
    var client = httpFactory.CreateClient();
    
    try {
        // 1st call: Will timeout (mocked)
        var response1 = await client.GetAsync("https://library.com/timeout-test");
    } catch (TaskCanceledException) {
        Console.WriteLine("✅ Timeout simulated successfully");
    }
    
    // 2nd call: Will succeed (mocked)
    var response2 = await client.GetAsync("https://library.com/timeout-test");
    var content = await response2.Content.ReadAsStringAsync();
    Console.WriteLine($"✅ Response: {content}");
    
    return Results.Ok(new { demo = "completed" });
});

app.Run();
```

### Custom Mock Scenario

```csharp
public class HttpMockUnauthorized : IHttpMockScenario {
    public Func<HttpRequestMessage, bool> Match => request =>
        request.RequestUri?.AbsoluteUri.Contains("secure-api.com") == true &&
        !request.Headers.Contains("Authorization");

    public IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory => 
        new List<Func<Task<HttpResponseMessage>>> {
            () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) {
                Content = new StringContent("{\"error\":\"Unauthorized\"}")
            })
        };
}
```

---

## 🎯 Best Practices

1. ✅ **Use `AddTransient`** for all mock registrations
2. ✅ **Keep `Match` logic simple** (avoid heavy operations)
3. ✅ **Use `MockBehavior.Strict`** to catch configuration errors early
4. ✅ **Test sequential scenarios** with multiple calls
5. ✅ **Use clear scenario names** (e.g., `HttpMock[Service][Scenario]`)

---

## 🔗 See Also

**🎉 Happy Mocking!** 🚀