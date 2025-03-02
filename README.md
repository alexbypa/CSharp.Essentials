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
    private readonly List<httpClientOptions> httpClientOptions;
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
        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
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
        actionsHttp.Add(traceRetry);
        var httpsClientHelper = new httpsClientHelper(actionsHttp);
        IContentBuilder contentBuilder = new NoBodyContentBuilder();

        var responseMessage = await httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .SendAsync(url, HttpMethod.Get, null, contentBuilder
        );
        return Ok(responses);
    }
    /// <summary>
    /// Call Simple Http Request with retry
    /// </summary>
    /// <param name="testRequest"></param>
    /// <returns></returns>
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
            .addRetryCondition((httpRes) => (int)httpRes.StatusCode == httpStausOnRetry, totRetry, secondsDelayEspOnRetry);

        var responseMessage = await httpsClientHelper
            .SendAsync(url, HttpMethod.Get, null, contentBuilder
        );

        return Ok(responses);
    }
    /// <summary>
    /// Call Simple Http Request with rate limit
    /// </summary>
    /// <param name="testRequest"></param>
    /// <returns></returns>
    [HttpGet("withRateLimit")]
    public async Task<IActionResult> withRateLimit(string url, int totIterations) {
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
            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit });
            return Task.CompletedTask;
        };
        actionsHttp.Add(traceRetry);
        var httpsClientHelper = new httpsClientHelper(actionsHttp);
        IContentBuilder contentBuilder = new NoBodyContentBuilder();

        httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .addRateLimitOnMoreRequests(httpClientOptions.Where(item => item.Name == "Test").FirstOrDefault()?.RateLimitOptions);

        for (int i = 0; i < totIterations; i++) {
            var responseMessage = await httpsClientHelper
                .SendAsync(url, HttpMethod.Get, null, contentBuilder
            );
        }
        return Ok(responses);
    }
    /// <summary>
    /// Call Simple Http Request a XML body
    /// </summary>
    /// <param name="testRequest"></param>
    /// <returns></returns>
    [HttpGet("withXmlBody")]
    public async Task<IActionResult> withXmlBody(string url, string content) {

        //var client = new HttpClient();
        //var request = new HttpRequestMessage(HttpMethod.Post, "https://reqbin.com/echo/post/xml");
        //request.Headers.Add("Accept", "application/xml");
        //request.Headers.Add("Content-Type", "application/xml");
        //var contentbody = new StringContent("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Request>\r\n    <Login>login</Login>\r\n    <Password>password</Password>\r\n</Request>", null, "application/xml");
        //request.Content = contentbody;
        //var response = await client.SendAsync(request);
        //return Ok(await response.Content.ReadAsStringAsync());


        var body = new XDocument();
        body.Add(new XElement("root", new XElement("child", "content")));

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
            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit, payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult() });
            return Task.CompletedTask;
        };
        actionsHttp.Add(traceRetry);
        var httpsClientHelper = new httpsClientHelper(actionsHttp);
        IContentBuilder contentBuilder = new XmlContentBuilder();

        var responseMessage = await httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .SendAsync(url, HttpMethod.Post, body.ToString(), contentBuilder);

        return Ok(responses);
    }
        
    /// <summary>
    /// Call Simple Http Request a json body
    /// </summary>
    /// <param name="testRequest"></param>
    /// <returns></returns>
    [HttpGet("withJsonBody")]
    public async Task<IActionResult> withJsonBody(string url, string content) {
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
            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit, payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult() });
            return Task.CompletedTask;
        };
        actionsHttp.Add(traceRetry);
        var httpsClientHelper = new httpsClientHelper(actionsHttp);
        IContentBuilder contentBuilder = new JsonContentBuilder();

        var responseMessage = await httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .SendAsync(url, HttpMethod.Post, content, contentBuilder);

        return Ok(responses);
    }

    /// <summary>
    /// Call Simple Http Request a json body
    /// </summary>
    /// <param name="testRequest"></param>
    /// <returns></returns>
    [HttpGet("withFormBody")]
    public async Task<IActionResult> withFormBody(string url) {
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
            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit, payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult() });
            return Task.CompletedTask;
        };
        actionsHttp.Add(traceRetry);
        var httpsClientHelper = new httpsClientHelper(actionsHttp);
        string content = "key1=value1&key2=value2&key3=value3";
        var form = content.Split("&").Select(x => x.Split("=")).ToDictionary(x => x[0], x => x[1]);
        IContentBuilder contentBuilder = new FormUrlEncodedContentBuilder();

        var responseMessage = await httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .SendAsync(url, HttpMethod.Post, form, contentBuilder);

        return Ok(responses);
    }

    [HttpPost("testAll")]
    public async Task<IActionResult> testAll([FromBody] HttpTestRequest testRequest) {
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
            loggerExtension<MyRequest>.TraceAsync(
                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
                Serilog.Events.LogEventLevel.Information,
                null,
                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                );
            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit, payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult() });
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
                httpmethod switch {
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
    public class MyRequest : IRequest {
        public string IdTransaction { get; set; }

        public string Action { get; set; }
    }
    public class HttpTestRequest {
        public string Url { get; set; }
        public object contentBody { get; set; }
        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
        public int totIterations { get; set; }
        public int httpStatusForRetry { get; set; }
        public int numberRetries { get; set; }
        public int secondsDelayEspOnRetry { get; set; }
    }
}
```

### Use cases
```http
@Test_HostAddress = http://localhost:5133
@Test_ExternalCall = https://webhook.site/YOUR-UNIQUE-ID

### 1) withNoBdoy: Call Simple Http Request
GET {{Test_HostAddress}}/httpHelper/withNoBdoy?url={{Test_ExternalCall}}
Accept: html/text
Content-Type: html/text

### 2) Call Simple Http Request with Rate Limit
GET {{Test_HostAddress}}/httpHelper/withRateLimit?url={{Test_ExternalCall}}&totIterations=4
Accept: html/text
Content-Type: html/text

### 3) Call Simple Http Request with retry
GET {{Test_HostAddress}}/httpHelper/withRetries?url={{Test_ExternalCall}}&httpStausOnRetry=429&totRetry=4&secondsDelayEspOnRetry=2
Accept: html/text
Content-Type: html/text

### 4) POST body XML 
GET {{Test_HostAddress}}/httpHelper/withXmlBody?url={{Test_ExternalCall}}&content=as
Accept: application/json
Content-Type: application/json

### 5) POST body json
GET {{Test_HostAddress}}/httpHelper/withJsonBody?url={{Test_ExternalCall}}&content={"item":"value"}&contentType=application/json
Accept: application/json
Content-Type: application/json


### 6) POST form x-www-form-urlencoded
GET {{Test_HostAddress}}/httpHelper/withFormBody?url={{Test_ExternalCall}}&content={"Key1":"value1"}&contentType=application/json
Accept: application/json
Content-Type: application/json


### 7) POST body json with ratelimit
POST {{Test_HostAddress}}/httpHelper/testAll
Accept: application/json
Content-Type: application/json

{
  "url": "{{Test_ExternalCall}}",
  "contentBody": "{\"nome\":\"Mario\"}",
  "contentType": "application/json",
  "HttpMethod": "POST",
  "totIterations":1,
  "httpStatusForRetry": 200,
  "numberRetries":4,
  "secondsDelayEspOnRetry": 2
}
```