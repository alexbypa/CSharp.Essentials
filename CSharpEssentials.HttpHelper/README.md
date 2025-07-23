# 🔗 CSharpEssentials.HttpHelper

A robust and extensible HTTP helper designed for modern .NET applications that require:

- HTTP Client pooling (via `HttpClientFactory`)
- Rate limiting
- Retry with Polly
- Fully testable, SOLID-friendly structure
- Plug & play integration via `IhttpsClientHelperFactory`

---

## 🚀 Setup

Install the package and configure via:

```csharp
services.AddHttpClients(Configuration);
````

Optionally provide an external configuration file:

```
appsettings.httpHelper.json
```

Sample:

```json
{
  "HttpClientOptions": [
    {
      "Name": "GitHubClient",
      "RateLimitOptions": {
        "PermitLimit": 5,
        "QueueLimit": 10,
        "Window": "00:00:10",
        "SegmentsPerWindow": 1,
        "AutoReplenishment": true
      }
    }
  ]
}
```

---

## 🔧 Usage

### Initialize the client:

```csharp
var httpHelper = httpFactory
    .CreateOrGet("GitHubClient")
    .AddRequestAction((req, res, retry, ts) => {
        Console.WriteLine($"[Retry: {retry}] {req.RequestUri}");
        return Task.CompletedTask;
    });
```

### Add headers and authentication:

```csharp
httpHelper.setHeadersAndBearerAuthentication(
    new Dictionary<string, string> { { "User-Agent", "MyGitHubApp" } },
    new httpsClientHelper.httpClientAuthenticationBearer("your_github_token"));
```

### Send request (GET / POST / PUT / DELETE):

```csharp
IContentBuilder contentBuilder = new NoBodyContentBuilder(); // or JsonContentBuilder
HttpResponseMessage response = await httpHelper.SendAsync(
    url: "https://api.github.com/search/repositories?q=recap+in:name",
    httpMethod: HttpMethod.Get,
    body: null,
    contentBuilder: contentBuilder
);
```

---

## 🔄 Retry Handling

```csharp
httpHelper.addRetryCondition(
    response => !response.IsSuccessStatusCode, retryCount: 3, backoffFactor: 2.0
);
```

---

## 🧪 GitHub API Usage Example

### Minimal API Endpoint (search repos):

```csharp
app.MapGet("/repos/search", async (
    [FromQuery] string Pattern,
    [FromServices] IhttpsClientHelperFactory httpFactory) => {

    var httpHelper = httpFactory.CreateOrGet("GitHubClient")
        .AddRequestAction((req, res, retry, ts) => {
            Console.WriteLine($"Executed request to {req.RequestUri}");
            return Task.CompletedTask;
        });

    httpHelper.setHeadersAndBearerAuthentication(
        new Dictionary<string, string> { { "User-Agent", "MyGitHubApp" } },
        new httpsClientHelper.httpClientAuthenticationBearer("your_github_token"));

    var url = $"https://api.github.com/search/repositories?q={Pattern}+in:name&per_page=10";

    var response = await httpHelper.SendAsync(url, HttpMethod.Get, null, new NoBodyContentBuilder());
    var json = await response.Content.ReadAsStringAsync();

    using var doc = JsonDocument.Parse(json);
    var repos = doc.RootElement
        .GetProperty("items")
        .EnumerateArray()
        .Select(repo => new {
            Name = repo.GetProperty("full_name").GetString(),
            Url = repo.GetProperty("html_url").GetString(),
            Description = repo.TryGetProperty("description", out var descProp) && descProp.ValueKind != JsonValueKind.Null
                ? descProp.GetString()
                : "(no description)"
        })
        .ToList();

    return Results.Ok(repos);
});
```

---

## 📦 Supported Content Builders

* `JsonContentBuilder` → for `application/json`
* `FormUrlEncodedContentBuilder` → for form data
* `XmlContentBuilder` → for `application/xml`
* `NoBodyContentBuilder` → for GET / DELETE

---

## 📈 Built-in Features

| Feature         | Description                                |
| --------------- | ------------------------------------------ |
| Retry           | Polly-based retry with exponential backoff |
| Rate Limiting   | Sliding window limiter per client instance |
| Headers/Auth    | Bearer / Basic / Custom headers            |
| Logging Handler | Custom DelegatingHandler logs all requests |
| Retry Info      | Injects `X-Retry-Attempt` and duration     |

---

## 📁 Folder Structure

* `httpsClientHelper.cs` – main engine
* `httpsClientHelperFactory.cs` – factory + DI integration
* `HttpRequestBuilder.cs` – fluent builder pattern
* `IContentBuilder.cs` – pluggable request body strategies
* `HttpClientHandlerLogging.cs` – optional delegating handler
* `httpClientOptions.cs` – config-based client tuning

---

## 🤝 Contributing

Pull requests are welcome. Please make sure to run unit tests and respect project structure before submitting.

---

## 📜 License

MIT

```
