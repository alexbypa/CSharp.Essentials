---
# **`AddRequestAction` – CSharpEssentials.HttpHelper**
*Intercept and customize HTTP requests with async callbacks*

---

## **📌 Overview**
`AddRequestAction` allows you to **register one or more async callback functions** that execute **after every HTTP request**, giving you access to:
- **Request (`HttpRequestMessage`)** – Details like URL, method, headers, and body.
- **Response (`HttpResponseMessage`)** – Status code, headers, and content.
- **Metadata** – Retry count (`retryCount`) and rate-limiting delay (`RateLimitTimeSpanElapsed`).

🔹 **Key Use Cases**:
✅ **Advanced Logging** (e.g., performance tracking).
✅ **API Monitoring** (e.g., counting failed requests).
✅ **Dynamic Response Modification** (e.g., adding custom headers).
✅ **Integration with External Systems** (e.g., sending metrics to Prometheus/Grafana).

---

## **🔧 Syntax**
```csharp
IhttpsClientHelper AddRequestAction(
    Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> action
)
```
| Parameter                     | Type                          | Description                                                                 |
|-------------------------------|-------------------------------|-----------------------------------------------------------------------------|
| `action`                      | `Func<..., Task>`             | Async function to execute after each request.                            |
| **`HttpRequestMessage req`**  | `HttpRequestMessage`         | The HTTP request object.                                                    |
| **`HttpResponseMessage res`** | `HttpResponseMessage`        | The HTTP response object.                                                   |
| **`int retryCount`**          | `int`                         | Number of retries attempted (if configured with `addRetryCondition`).      |
| **`TimeSpan elapsed`**        | `TimeSpan`                    | Time elapsed due to rate limiting (if enabled).                            |

---

## **🚀 High-Impact Demo**
### **1. Minimal API with Custom Logging**
```csharp
using CSharpEssentials.HttpHelper;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddTransient<IhttpsClientHelperFactory, httpsClientHelperFactory>();

var app = builder.Build();

// Endpoint to test AddRequestAction
app.MapGet("/api/test-logging", async (IhttpsClientHelperFactory httpFactory) =>
{
    // 1. Create an HTTP client with custom logging
    var client = httpFactory.CreateClient("httpbin")
        .AddRequestAction(async (req, res, retry, elapsed) =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[LOG] {req.Method} {req.RequestUri} → {res.StatusCode}");
            Console.WriteLine($"     Retry: {retry}, RateLimit Delay: {elapsed.TotalMilliseconds}ms");
            Console.ResetColor();
        });

    // 2. Execute a GET request to httpbin.org
    var response = await client.SendAsync(
        "https://httpbin.org/get",
        HttpMethod.Get,
        body: null,
        contentBuilder: new NoBodyContentBuilder(),
        headers: null,
        cancellationToken: default
    );

    return Results.Ok(await response.Content.ReadAsStringAsync());
})
.WithName("TestHttpLogging")
.WithTags("HTTP Helper");

app.Run();
```

---

### **2. Expected Logging Output**
When calling `/api/test-logging`, you’ll see in the terminal:

## **🔍 Implementation Details**
### **How It Works Internally?**
1. **Callback Registration**:
   `AddRequestAction` adds the function to the `_callbacks` list in `HttpRequestEvents`.
2. **Post-Request Execution**:
   After every `SendAsync`, `HttpClientHandlerLogging` invokes all registered callbacks with:
   ```csharp
   await _events.InvokeAll(request, response, totRetry, RateLimitTimeSpanElapsed);
   ```
3. **Thread Safety**:
   Callbacks are executed asynchronously via `Task.WhenAll`, ensuring non-blocking behavior.

---

## **🌍 Global Best Practices**
1. **Logging**:
   - Use structured logging (e.g., Serilog) for production.
   - Avoid blocking I/O in callbacks (e.g., `File.AppendAllTextAsync` is fine for demos but not for high-load systems).
2. **Error Handling**:
   - Wrap callback logic in `try-catch` to avoid crashing the pipeline.
3. **Performance**:
   - Minimize heavy operations in callbacks (e.g., database writes).
   - Use `CancellationToken` to respect timeouts.

---


## **💡 Pro Tips**
- **Combine with `IHttpRequestEvents`**:
  Clear all callbacks with `ClearRequestActions()` when needed.
- **Use for A/B Testing**:
  Modify responses dynamically (e.g., inject headers for feature flags).
