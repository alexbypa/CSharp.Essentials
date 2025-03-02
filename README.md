# CSharpEssentials Library

CSharpEssentials is a collection of NuGet packages that provide a range of helpers and utilities for .NET development. This library includes various packages designed to simplify tasks such as HTTP operations, background job scheduling, logging, and more.

## Package Index

Below is a table listing all the current packages available in the CSharpEssentials library:

| **Package Name**                         | **Description**                                                                              | **NuGet Link**                                                        |
|------------------------------------------|----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| [CSharpEssentials.HttpHelper] (#csharpessentials.httphelper)          | Provides helper methods and extensions for simplified HTTP client operations.               | [NuGet](https://www.nuget.org/packages/CSharpEssentials.HttpHelper)    |
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
## CSharpEssentials.HttpHelper

CSharpEssentials.HttpHelper is a NuGet package that extends HttpClient functionalities by integrating resiliency and rate limiting strategies. 
With this package, HTTP calls in your .NET applications become more robust, handling transient errors and request limitations gracefully.

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
dotnet add package CSharpEssentials.HttpHelper --version 1.2.2
```

### Create controller like below

```csharp
using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Test.Controllers.httphelper;
[ApiController]
[Route("httpHelper")]
public class httphelperController : Controller {
    private readonly List<httpClientOptions> httpClientOptions;
    public httphelperController(IOptions<List<httpClientOptions>> httpClientOptions) {
        this.httpClientOptions = httpClientOptions.Value;
    }

    [HttpPost("httptest")]
    public async Task<IActionResult> httptest([FromBody] HttpTestRequest testRequest) {
        string url = testRequest.Url;
        object contentBody = testRequest.contentBody;
        string httpmethod = testRequest.HttpMethod;
        string contentType = testRequest.ContentType;
        int httpStatusForRetry = testRequest.httpStatusForRetry;
        int numberRetries = testRequest.numberRetries;
        int secondsDelayEspOnRetry = testRequest.secondsDelayEspOnRetry;

        List<object> responses = new List<object>();
        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
            loggerExtension<MiaRichiesta>.TraceAsync(
                new MiaRichiesta { Action = "Test Http", IdTransaction = "Alex" },
                Serilog.Events.LogEventLevel.Information,
                null,
                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                );
            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit });
            return Task.CompletedTask;
        };
        actionsHttp.Add(traceRetry);
        var httpsClientHelper = new httpsClientHelper(actionsHttp);
        if (contentType.Equals("application/x-www-form-urlencoded", StringComparison.InvariantCultureIgnoreCase)) {
            contentBody = contentBody.ToString().TrimStart('?');
            contentBody = contentBody.ToString().Split("&").Select(x => x.Split("=")).ToDictionary(x => x[0], x => x[1]);
        }
        // 2) Seleziona l’IContentBuilder in modo fluente
        IContentBuilder contentBuilder = (contentBody, contentType) switch {
            (null or "", _) => new NoBodyContentBuilder(),
            (_, "application/json") => new JsonContentBuilder(),
            (_, "application/x-www-form-urlencoded") => new FormUrlEncodedContentBuilder(),
            (_, "application/xml") => new XmlContentBuilder(),
            _ => new NoBodyContentBuilder()
        };

        httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .addRetryCondition((httpRes) => (int)httpRes.StatusCode == httpStatusForRetry, numberRetries, secondsDelayEspOnRetry)
            .addRateLimitOnMoreRequests(httpClientOptions.FirstOrDefault()?.RateLimitOptions);

        //Task<HttpResponseMessage> responseMessage = httpsClientHelper.sendAsync(url);
        for (int i = 0; i < testRequest.totIterations; i++) {
            Task<HttpResponseMessage> responseMessage = httpsClientHelper.SendAsync(
                url,
                httpmethod switch{
                    "POST" => HttpMethod.Post,
                    "post" => HttpMethod.Post,
                    "get" => HttpMethod.Get,
                    _ => HttpMethod.Get
                },
                contentBody,
                contentBuilder
                );
            var responseHttp = responseMessage.GetAwaiter().GetResult();
            string content = $"{responseHttp.StatusCode}: {responseHttp.Content.ReadAsStringAsync().GetAwaiter().GetResult()}";
        }

        return Ok(responses);
    }
    public class MiaRichiesta : IRequest {
        public string IdTransaction { get; set; }

        public string Action { get; set; }
    }
    public class HttpTestRequest {
        public string Url { get; set; }
        public object contentBody { get; set; }
        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
        public int totIterations{ get; set; }
        public int httpStatusForRetry { get; set; }
        public int numberRetries { get; set; }
        public int secondsDelayEspOnRetry { get; set; }
    }
}
```

### Use cases
```http
@Test_HostAddress = http://localhost:5133
@Test_ExternalCall = https://webhook.site/985bc317-0c0e-4186-a2f7-19f19f13a0d8

### 1) POST body json with ratelimit
POST {{Test_HostAddress}}/httpHelper/httptest
Accept: application/json
Content-Type: application/json

{
  "url": "{{Test_ExternalCall}}",
  "contentBody": "{\"nome\":\"Mario\"}",
  "contentType": "application/json",
  "HttpMethod": "POST",
  "totIterations":4,
  "httpStatusForRetry": 500,
  "numberRetries":4,
  "secondsDelayEspOnRetry": 2
}

### 2) POST body XML with retry
POST {{Test_HostAddress}}/httpHelper/httptest
Accept: application/json
Content-Type: application/json

{
  "url": "{{Test_ExternalCall}}",
  "contentBody": "<radice><figlio>ciccio</figlio></radice>",
  "contentType": "application/xml",
  "HttpMethod": "POST",
  "totIterations":1,
  "httpStatusForRetry": 200,
  "numberRetries":4,
  "secondsDelayEspOnRetry": 2
}

### 2) POST FORM x-www-form-urlencoded
POST {{Test_HostAddress}}/httpHelper/httptest
Accept: application/json
Content-Type: application/json

{
  "url": "{{Test_ExternalCall}}",
  "contentBody": "nome1=ciccio&nome2=frank",
  "contentType": "application/x-www-form-urlencoded",
  "HttpMethod": "POST",
  "totIterations":1,
  "httpStatusForRetry": 500,
  "numberRetries":4,
  "secondsDelayEspOnRetry": 2
}

```
