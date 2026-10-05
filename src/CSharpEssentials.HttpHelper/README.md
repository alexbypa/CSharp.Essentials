# CSharpEssentials.HttpHelper

![Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue)
![CodeQL](https://github.com/alexbypa/CSharp.Essentials/actions/workflows/codeqlLogger.yml/badge.svg)
![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.HttpHelper.svg)
![Downloads](https://img.shields.io/nuget/dt/CSharpEssentials.HttpHelper.svg)
![Last Commit](https://img.shields.io/github/last-commit/alexbypa/CSharp.Essentials?style=flat-square)
![GitHub Discussions](https://img.shields.io/github/discussions/alexbypa/CSharp.Essentials)
![Issues](https://img.shields.io/github/issues/alexbypa/CSharp.Essentials)

**Targets:** `net8.0` · `net9.0` · `net10.0`

`CSharpEssentials.HttpHelper` wraps `IHttpClientFactory` with a small fluent API: named clients configured in `appsettings.json`, retry (Polly), per-call timeout, rate limiting, per-request callbacks and built-in mocking for tests. Transport failures come back as a `HttpResponseMessage` (408 / 502 / 500) instead of exceptions.

- [Quick Start](#-quick-start)
- [Features](#-features)
- [Examples](#-examples) — JSON, form, headers & auth, retry, timeout, callbacks
- [Comparison](#-comparison-with-refit-flurl-and-plain-httpclient)
- [Documentation & Demo](#-documentation--demo)

---

## 🚀 Quick Start

**1. Install**

```bash
dotnet add package CSharpEssentials.HttpHelper
```

**2. Configure a named client** in `appsettings.json`:

```json
{
  "HttpClientOptions": [
    { "Name": "github", "UseCompression": true }
  ]
}
```

**3. Register** in `Program.cs`:

```csharp
builder.Services.AddHttpClients(builder.Configuration);
```

**4. Call** — inject `IhttpsClientHelperFactory`, then `CreateOrGet(name)`:

```csharp
public class GitHubService(IhttpsClientHelperFactory factory)
{
    public async Task<string> GetUserAsync(string user, CancellationToken ct)
    {
        var response = await factory.CreateOrGet("github")
            .addRetryCondition(r => (int)r.StatusCode >= 500, retryCount: 3, backoffFactor: 2)
            .addTimeout(TimeSpan.FromSeconds(10))
            .SendAsync($"https://api.github.com/users/{user}", HttpMethod.Get,
                       headers: new Dictionary<string, string> { ["User-Agent"] = "my-app" },
                       cancellationToken: ct);

        return await response.Content.ReadAsStringAsync(ct);
    }
}
```

`CreateOrGet` returns the same helper for the same name, so its settings (retry, timeout, headers, callbacks) stick across calls.

---

## ✨ Features

| Feature | How | Notes |
|---|---|---|
| Named clients from config | `HttpClientOptions` array + `AddHttpClients(configuration)` | Built on `IHttpClientFactory` (pooled handlers, no socket exhaustion) |
| Retry with backoff | `addRetryCondition(predicate, retryCount, backoffFactor)` | Delay before retry *n* = `backoffFactor`ⁿ seconds. Adds an `X-Retry-Attempt` header |
| Timeout | `addTimeout(TimeSpan)` | Expiry returns **408** `{"error":"timeout",...}`; defaults to the client's `HttpClient.Timeout` |
| Rate limiting | `RateLimitOptions` in config (sliding window) | Over the limit returns **429** `{"error":"rate_limit_exceeded"}` |
| Request callbacks | `AddRequestAction(...)` / `factory.AddActionOnRequest(...)` | Per client or global; receives request, response, retry attempt, elapsed |
| Body builders | `JsonContentBuilder`, `XmlContentBuilder`, `FormUrlEncodedContentBuilder`, `NoBodyContentBuilder` | Implement `IContentBuilder` for anything else |
| Headers & auth | `addHeaders`, `setHeadersAndBearerAuthenticationSync`, `setHeadersAndBasicAuthenticationSync`, per-call `headers` | |
| Compression, proxy, client certificate | `UseCompression`, `httpProxy`, `Certificate` in config | Configured on the pooled `SocketsHttpHandler` |
| Logging | Failures are logged through [LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) | Companion package, same Serilog pipeline |
| Mocking | Register `IHttpMockScenario` instances in DI | Matching requests never reach the network; see [Testing](#-testing-with-mock-scenarios) |

**No exceptions for transport failures.** `SendAsync` returns:

| Situation | Result |
|---|---|
| Timeout (`addTimeout` or `HttpClient.Timeout`) | `408 RequestTimeout` |
| Rate limit exceeded | `429 TooManyRequests` |
| `HttpRequestException` (DNS, connection refused…) | `502 BadGateway` |
| Any other exception | `500 InternalServerError` |
| **Caller cancels** the `CancellationToken` | `OperationCanceledException` is thrown (standard .NET contract) |

---

## 📚 Examples

### JSON body

`JsonContentBuilder` sends `body.ToString()` as `application/json`, so pass an **already serialized string**:

```csharp
using System.Text.Json;

var json = JsonSerializer.Serialize(new { name = "Alice", age = 30 });

var response = await factory.CreateOrGet("api")
    .SendAsync("https://api.example.com/users", HttpMethod.Post,
               new JsonContentBuilder(), json);

response.EnsureSuccessStatusCode();
var created = await response.Content.ReadFromJsonAsync<User>();   // System.Net.Http.Json
```

### Form data

```csharp
var form = new Dictionary<string, string> { ["grant_type"] = "client_credentials", ["scope"] = "read" };

var response = await factory.CreateOrGet("auth")
    .SendAsync("https://auth.example.com/token", HttpMethod.Post,
               new FormUrlEncodedContentBuilder(), form);
```

### Fluent: headers, auth, retry, timeout

Every configuration method returns the helper, so settings chain:

```csharp
var helper = factory.CreateOrGet("api")
    .setHeadersAndBearerAuthenticationSync(
        new Dictionary<string, string> { ["Accept"] = "application/json" },
        new httpsClientHelper.httpClientAuthenticationBearer(token))
    .addRetryCondition(r => r.StatusCode == HttpStatusCode.TooManyRequests || (int)r.StatusCode >= 500,
                       retryCount: 3, backoffFactor: 2)       // waits 2s, 4s, 8s
    .addTimeout(TimeSpan.FromSeconds(5));

var response = await helper.SendAsync("https://api.example.com/orders", HttpMethod.Get);
```

> `setHeaders…Sync` replaces the client's default headers. Per-request headers (the `headers:` argument of `SendAsync`) don't touch shared state — prefer them when the same named client is used concurrently.

### Rate limiting and compression (config)

```json
{
  "HttpClientOptions": [
    {
      "Name": "api",
      "UseCompression": true,
      "RateLimitOptions": {
        "IsEnabled": true,
        "PermitLimit": 10,
        "Window": "00:00:01",
        "SegmentsPerWindow": 2,
        "QueueLimit": 0,
        "AutoReplenishment": true
      }
    }
  ]
}
```

### Per-request callbacks

```csharp
factory.CreateOrGet("api").AddRequestAction(async (request, response, retryAttempt, elapsed) =>
{
    Console.WriteLine($"{request.Method} {request.RequestUri} -> {(int)response.StatusCode} (retry #{retryAttempt})");
    await Task.CompletedTask;
});

// all clients:
factory.AddActionOnRequest((req, res, attempt, elapsed) => Task.CompletedTask);
```

A throwing callback is logged and ignored — it never fails the HTTP call.

### 🧪 Testing with mock scenarios

Register a scenario in DI; requests it matches are answered by your factories (round-robin) and never hit the network:

```csharp
services.AddSingleton<IHttpMockScenario>(new HttpMockScenario(
    match: r => r.RequestUri?.Host == "mock.local",
    responseFactory: new List<Func<Task<HttpResponseMessage>>>
    {
        () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)),
        () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
    }));
services.AddHttpClients(configuration);
```

With `addRetryCondition(r => r.StatusCode == HttpStatusCode.TooManyRequests, 3, 0)` the first call returns 429, the retry returns 200. Last matching scenario wins; a factory may also throw `HttpRequestException` to simulate a network failure.

Mocks honour timeout and caller cancellation. Use the `(request, ct) => ...` overload to observe the token:

```csharp
services.AddSingleton<IHttpMockScenario>(new HttpMockScenario(
    match: r => r.RequestUri?.Host == "slow.local",
    responseFactory: new List<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>
    {
        async (request, ct) => {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        },
    }));
```

With `addTimeout(TimeSpan.FromSeconds(1))` the call returns 408; cancelling the caller's token throws `OperationCanceledException`. The token inside the factory is only available via `HttpMockScenario`; custom `IHttpMockScenario` implementations still get timeout/cancellation (the engine stops waiting) but not the token in their factory.

---

## ⚖️ Comparison with Refit, Flurl and plain HttpClient

They solve different problems; this table is about **what you get out of the box**.

| | **HttpHelper** | **Refit** | **Flurl** | **Plain `HttpClient`** |
|---|---|---|---|---|
| Style | Named client + fluent helper | Typed interface (`[Get("/users/{id}")]`) | Fluent URL builder | Manual |
| Setup | `HttpClientOptions` in appsettings | Interface + `AddRefitClient<T>()` | None (static `Url`/`FlurlClient`) | `AddHttpClient()` |
| Built on `IHttpClientFactory` | Yes | Yes | Own client cache | Yes |
| Retry / backoff | Built in (Polly) | Add Polly / resilience handler | Add Polly or custom handler | Add Polly / resilience handler |
| Timeout → response | 408 response | `TaskCanceledException` | `FlurlHttpTimeoutException` | `TaskCanceledException` |
| Transport errors | Returned as 502 / 500 response | Exception (`ApiException`) | Exception (`FlurlHttpException`) | Exception |
| Rate limiting | Built in (config) | — | — | Add `RateLimiter` handler |
| Logging of calls | Built in (LoggerHelper) | `ILogger` / handler | Event handlers | `ILogger` / handler |
| JSON (de)serialization | You serialize; read with `ReadFromJsonAsync` | Automatic, typed | Automatic (`PostJsonAsync`, `ReceiveJson<T>`) | `System.Net.Http.Json` |
| Compile-time typed API | No | **Yes** | No | No |
| Mocking in tests | Mock scenarios via DI | Mock `HttpMessageHandler` | `HttpTest` | Mock `HttpMessageHandler` |

**Pick HttpHelper** when you call a handful of third-party APIs and want retry, timeout, rate limit and logging configured in one place, with failures as plain status codes. **Pick Refit** for a large, stable API surface you want typed. **Pick Flurl** for ad-hoc fluent calls with automatic JSON. They can coexist in one app.

---

## 📖 Documentation & Demo

- [HttpHelper page](https://www.loggerhelper.it/httphelper.html) — Quick Start, examples, comparison
- [Full site](https://www.loggerhelper.it)

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!
Feel free to open a [pull request](https://github.com/alexbypa/CSharp.Essentials/pulls) or [issue](https://github.com/alexbypa/CSharp.Essentials/issues).

## 📜 License

Distributed under the MIT License. See [LICENSE](../LICENSE) for more information.
