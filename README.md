# CSharpEssentials Library

CSharpEssentials is a collection of NuGet packages that provide a range of helpers and utilities for .NET development. This library includes various packages designed to simplify tasks such as HTTP operations, background job scheduling, logging, and more.

## Package Index

Below is a table listing all the current packages available in the CSharpEssentials library:

| **Package Name**                         | **Description**                                                                              | **NuGet Link**                                                        |
|------------------------------------------|----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| [CSharpEssentials.HttpHelper](#csharpessentialshttphelper)          | Provides helper methods and extensions for simplified HTTP client operations.               | [NuGet](https://www.nuget.org/packages/CSharpEssentials.HttpHelper)    |
| **CSharpEssentials.HangFireHelper** (#csharpessentialshangfirehelper)      | Contains utilities and extensions for integrating Hangfire background job processing.         | [NuGet](https://www.nuget.org/packages/CSharpEssentials.HangFireHelper)|
| **CSharpEssentials.LoggerHelper** (#csharpessentialsloggerhelper)       | Offers logging helpers to trace requests using various Serilog sinks and logging strategies. | [NuGet](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)  |
| **...**                                  | ... (more packages coming soon)                                                              | ...                                                                   |

## Overview

The CSharpEssentials library is designed to be modular, offering dedicated packages for distinct aspects of .NET development:

- **HttpHelper:** Simplifies HTTP client usage with additional resiliency features.
- **HangFireHelper:** Streamlines the integration and management of background jobs using Hangfire.
- **LoggerHelper:** Enhances logging capabilities by leveraging various logging sinks (e.g., Serilog).

Each package is intended to be used independently or in combination, depending on your project's needs.

## Getting Started

To install any of the packages, use the NuGet Package Manager or the .NET CLI. For example, to install the HttpHelper package:

```bash
dotnet add package CSharpEssentials.HttpHelper --version 1.2.2
```
## [CSharpEssentials.HttpHelper](#csharpessentialshttphelper)

CSharpEssentials.HttpHelper is a NuGet package that extends HttpClient functionalities by integrating resiliency and rate limiting strategies. 
With this package, HTTP calls in your .NET applications become more robust, handling transient errors and request limitations gracefully.

### Configuration (appsettings.json)
```js
{
  "HttpClientOptions": [
    {
      "name": "Test",
      "certificate": {
        "path": "YOUR_PATH",
        "password": "YOUR_PASSWORD"
      },
      "RateLimitOptions": {
        "AutoReplenishment": true,
        "PermitLimit": 1, 
        "QueueLimit": 1, 
        "Window": "00:00:15",
        "SegmentsPerWindow": 100
      }
    }
  ]
}
```
### 🌍 `HttpClientOptions`
📌 **explanation** (The package uses the following rate limiting algorithm: Sliding window limiter)

| Key | Type | Description |
|--------|------|-------------|
| `name` | `string` | Friendly name that encloses the settings to use with http to use a certificate for calls in https and configurations on the rate limit |
| `certificate` | `string` | Settings to upload the certificate |
| `certificate.path` | `string` | certificate path |
| `certificate.password` | `string` | password path |
| `RateLimitOptions.PermitLimit` | `int` | How many requests per window |
| `RateLimitOptions.QueueLimit` | `int` | maximum number of requests that can be waiting in the queue |
| `RateLimitOptions.Window` | `timestamp` | Time frame between one request and another |
| `RateLimitOptions.SegmentsPerWindow` | `int` | Number of segments per Window |


### Features

- **Resiliency:** Implements retry and fallback policies to manage transient errors.
- **Rate Limiting:** Controls the frequency of HTTP requests to prevent overloads and adhere to API limits.
- **Logging:** Leverages [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper) to trace requests using various Serilog sinks.
- **Custom Delegates:** Utilizes a custom handler to attach multiple delegates during the HTTP call for additional processing and customization.
- **Easy Integration:** Seamlessly integrates with ASP.NET Core applications.
- **Tested & Validated:** Verified using both an ASP.NET Core controller and `.http` file examples.

### Installation

Install the package via the NuGet Package Manager:

```bash
dotnet add package CSharpEssentials.HttpHelper --version 1.2.4
```

### API Examples
#### Example 1: Simple Get without body
```csharp
using Azure;
using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Test.Controllers.httphelper;
[ApiController]
[Route("httpHelper")]
public class httphelperController : Controller {
    private readonly List<httpClientOptions> httpClientOptions; // 1
    public httphelperController(IOptions<List<httpClientOptions>> httpClientOptions) {
        this.httpClientOptions = httpClientOptions.Value;
    }

    /// <summary>
    /// Call Simple Http Request
    /// </summary>
    /// <param name="testRequest"></param>
    /// <returns></returns>
    [HttpGet("withNoBdoy")]
    public async Task<IActionResult> withNoBdoy(string url) {
        List<object> responses = new List<object>();
        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();  // 2
        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> insertLog = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
            loggerExtension<MyRequest>.TraceAsync(
                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
                Serilog.Events.LogEventLevel.Information,
                null,
                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                );
            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit });
            return Task.CompletedTask;
        };
        actionsHttp.Add(insertLog); // 3
        var httpsClientHelper = new httpsClientHelper(actionsHttp);
        IContentBuilder contentBuilder = new NoBodyContentBuilder(); // 4

        var responseMessage = await httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30)) 
            .SendAsync(url, HttpMethod.Get, null, contentBuilder
        );
        return Ok(responses);
    }
}
```

#### Code Explanation

1. **`[httpClientOptions]`**  
  Let's load the appSettings configurations
2. **`[actionsHttp]`**  
  Defines the base route for the controller. The `[controller]` token is replaced with the actual class name (`Sample`), excluding the `Controller` suffix.

3. **`[insertLog]`**  
  Specifies that this method handles HTTP GET requests for the `hello` endpoint. The full URL becomes `api/Sample/hello`.
4. **`IContentBuilder`**  
  Defines the action method that handles the request. Returning `IActionResult` allows flexibility in returning different HTTP response types.
---

#### Example HTTP Request

```http
@Test_HostAddress = http://localhost:5133
@Test_ExternalCall = https://webhook.site/YOUR-UNIQUE-ID

### 1) withNoBdoy: Call Simple Http Request
GET {{Test_HostAddress}}/httpHelper/withNoBdoy?url={{Test_ExternalCall}}
Accept: html/text
Content-Type: html/text
```

#### Example 2: Simple with Retries
```csharp
    [HttpGet("withRetries")]
    public async Task<IActionResult> withRetries(string url, int httpStausOnRetry, int totRetry, int secondsDelayEspOnRetry) {
        List<object> responses = new List<object>();
        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
            loggerExtension<MyRequest>.TraceAsync(
                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
                Serilog.Events.LogEventLevel.Information,
                null,
                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                );
            responses.Add(new { content = httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult(), httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit });
            return Task.CompletedTask;
        };
        actionsHttp.Add(traceRetry);
        var httpsClientHelper = new httpsClientHelper(actionsHttp);
        IContentBuilder contentBuilder = new NoBodyContentBuilder(); 

        httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .addRetryCondition((httpRes) => (int)httpRes.StatusCode == httpStausOnRetry, totRetry, secondsDelayEspOnRetry); // 2

        var responseMessage = await httpsClientHelper
            .SendAsync(url, HttpMethod.Get, null, contentBuilder
        );

        return Ok(responses);
    }
```

#### Code Explanation

2. **`[addRetryCondition]`**  
  In this case we add the addRetryCondition method where we can choose when the retry should be performed, the number of retries (totRetry parameter), and the number of seconds in exponential mode (secondsDelayEspOnRetry parameter).
In the example of the next call a retry is performed when the httpStatus response is 409 (of course we can always choose different rules), for a maximum of 4 retries and with a delay between each of respectively: 2 seconds, 4, 8 and 16
---

#### Example HTTP Request

```http
GET {{Test_HostAddress}}/httpHelper/withRetries?url={{Test_ExternalCall}}&httpStausOnRetry=429&totRetry=4&secondsDelayEspOnRetry=2
Accept: html/text
Content-Type: html/text
```



