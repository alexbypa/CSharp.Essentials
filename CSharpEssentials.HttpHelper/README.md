# 🔗 CSharpEssentials.HttpHelper

A powerful and fluent **HTTP helper for .NET** built on top of `HttpClientFactory`, designed to simplify REST API calls with built-in support for **retry policies, rate limiting, logging, timeouts**, and dynamic configuration from `appsettings`.

---

## 🚀 Features

✅ Fully configurable via `appsettings.httpHelper.json`  
✅ Integrated with `HttpClientFactory`  
✅ Automatic retry with [Polly](https://github.com/App-vNext/Polly)  
✅ Rate Limiting using `SlidingWindowRateLimiter`  
✅ Support for `FormUrlEncodedContent` and JSON  
✅ Custom event-based logging  
✅ Fluent API: `.addTimeout()`, `.addRetryCondition()`, `.addHeaders()`, etc.

---

## 🛠️ Setup

### 1. In `Program.cs`:

```csharp
builder.Services.AddHttpClients(builder.Configuration);
````

### 2. Create or extend `appsettings.httpHelper.json`:

```json
{
  "HttpClientOptions": [
    {
      "Name": "Test1",
      "RateLimitOptions": {
        "AutoReplenishment": true,
        "PermitLimit": 1,
        "QueueLimit": 0,
        "Window": "00:00:15",
        "SegmentsPerWindow": 1
      }
    }
  ]
}
```

---

## 🧩 Usage Example

```csharp
var httpsClientHelper = (httpsClientHelper)factory.CreateOrGet("Test1");

var response = await httpsClientHelper
    .addTimeout(TimeSpan.FromSeconds(30))
    .AddRequestAction(async (req, res, retry, ts) => {
        Console.WriteLine($"[{req.Method}] {req.RequestUri} → {(int)res.StatusCode} | RETRY: {retry} | RL Wait: {ts}");
    })
    .addRetryCondition(
        res => res.StatusCode != HttpStatusCode.OK,
        retryCount: 3,
        backoffFactor: 2
    )
    .SendAsync(
        "https://example.com/api",
        HttpMethod.Get,
        null,
        new NoBodyContentBuilder()
    );

string body = await response.Content.ReadAsStringAsync();
```

---

## 📡 Auto-generated Headers

| Header                        | Description                                    |
| ----------------------------- | ---------------------------------------------- |
| `X-Retry-Attempt`             | Number of retries attempted                    |
| `X-RateLimit-TimeSpanElapsed` | Elapsed wait time due to rate limiter (if any) |

---

## ⚙️ Fluent Extensions

```csharp
httpsClientHelper
    .addTimeout(TimeSpan.FromSeconds(15))
    .addHeaders("Authorization", "Bearer your-token")
    .addFormData(new List<KeyValuePair<string, string>> { ... })
    .addRetryCondition(...)
    .AddRequestAction(...);
```

---

## 🧪 Rate Limit Testing

To test rate limiting, trigger multiple concurrent calls using the same configured client (e.g., `"Test1"`) and observe how the helper handles the cooldown period automatically using `SlidingWindowRateLimiter`.

---

## 🧰 Dynamic Registration

All HTTP clients are dynamically registered based on your `appsettings.httpHelper.json` configuration — no code changes required to add more.

---

## 🤝 Contributing

Pull requests, feedback, and improvements are welcome.
This package is part of the **CSharpEssentials** ecosystem.

---

## 📦 Requirements

* .NET 8.0 or higher
* NuGet Packages: `Polly`, `Microsoft.Extensions.Http`, `Microsoft.Extensions.Options`

---

## 📄 License

MIT

```
