//using Azure;
//using CSharpEssentials.HttpHelper;
//using CSharpEssentials.LoggerHelper;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using System;
//using System.Runtime.Serialization;
//using System.Xml.Linq;

//namespace Test.Controllers.httphelper;
//[ApiController]
//[Route("httpHelper")]
//public class httphelperController : Controller {
//    private readonly List<httpClientOptions> httpClientOptions;
//    public httphelperController(IOptions<List<httpClientOptions>> httpClientOptions) {
//        this.httpClientOptions = httpClientOptions.Value;
//    }

//    /// <summary>
//    /// Call Simple Http Request
//    /// </summary>
//    /// <param name="testRequest"></param>
//    /// <returns></returns>
//    [HttpGet("withNoBdoy")]
//    public async Task<IActionResult> withNoBdoy(string url) {
//        List<object> responses = new List<object>();
//        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
//        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
//            loggerExtension<MyRequest>.TraceAsync(
//                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
//                Serilog.Events.LogEventLevel.Information,
//                null,
//                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
//                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
//                );
//            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit });
//            return Task.CompletedTask;
//        };
//        actionsHttp.Add(traceRetry);
//        var httpsClientHelper = new httpsClientHelper(actionsHttp);
//        IContentBuilder contentBuilder = new NoBodyContentBuilder();

//        var responseMessage = await httpsClientHelper
//            .addTimeout(TimeSpan.FromSeconds(30))
//            .SendAsync(url, HttpMethod.Get, null, contentBuilder
//        );
//        return Ok(responses);
//    }
//    /// <summary>
//    /// Call Simple Http Request with retry
//    /// </summary>
//    /// <param name="testRequest"></param>
//    /// <returns></returns>
//    [HttpGet("withRetries")]
//    public async Task<IActionResult> withRetries(string url, int httpStausOnRetry, int totRetry, int secondsDelayEspOnRetry) {
//        List<object> responses = new List<object>();
//        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
//        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
//            loggerExtension<MyRequest>.TraceAsync(
//                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
//                Serilog.Events.LogEventLevel.Information,
//                null,
//                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
//                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
//                );
//            responses.Add(new { content = httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult(), httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit });
//            return Task.CompletedTask;
//        };
//        actionsHttp.Add(traceRetry);
//        var httpsClientHelper = new httpsClientHelper(actionsHttp);
//        IContentBuilder contentBuilder = new NoBodyContentBuilder();

//        httpsClientHelper
//            .addTimeout(TimeSpan.FromSeconds(30))
//            .addRetryCondition((httpRes) => (int)httpRes.StatusCode == httpStausOnRetry, totRetry, secondsDelayEspOnRetry);

//        var responseMessage = await httpsClientHelper
//            .SendAsync(url, HttpMethod.Get, null, contentBuilder
//        );

//        return Ok(responses);
//    }
//    /// <summary>
//    /// Call Simple Http Request with rate limit
//    /// </summary>
//    /// <param name="testRequest"></param>
//    /// <returns></returns>
//    [HttpGet("withRateLimit")]
//    public async Task<IActionResult> withRateLimit(string url, int totIterations) {
//        List<object> responses = new List<object>();
//        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
//        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
//            loggerExtension<MyRequest>.TraceAsync(
//                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
//                Serilog.Events.LogEventLevel.Information,
//                null,
//                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
//                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
//                );
//            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit });
//            return Task.CompletedTask;
//        };
//        actionsHttp.Add(traceRetry);
//        var httpsClientHelper = new httpsClientHelper(actionsHttp);
//        IContentBuilder contentBuilder = new NoBodyContentBuilder();

//        httpsClientHelper
//            .addTimeout(TimeSpan.FromSeconds(30))
//            .addRateLimitOnMoreRequests(httpClientOptions.Where(item => item.Name == "Test").FirstOrDefault()?.RateLimitOptions);

//        for (int i = 0; i < totIterations; i++) {
//            var responseMessage = await httpsClientHelper
//                .SendAsync(url, HttpMethod.Get, null, contentBuilder
//            );
//        }
//        return Ok(responses);
//    }
//    /// <summary>
//    /// Call Simple Http Request a XML body
//    /// </summary>
//    /// <param name="testRequest"></param>
//    /// <returns></returns>
//    [HttpGet("withXmlBody")]
//    public async Task<IActionResult> withXmlBody(string url, string content) {
//        var body = new XDocument();
//        body.Add(new XElement("root", new XElement("child", "content")));

//        List<object> responses = new List<object>();
//        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
//        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
//            loggerExtension<MyRequest>.TraceAsync(
//                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
//                Serilog.Events.LogEventLevel.Information,
//                null,
//                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
//                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
//                );
//            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit, payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult() });
//            return Task.CompletedTask;
//        };
//        actionsHttp.Add(traceRetry);
//        var httpsClientHelper = new httpsClientHelper(actionsHttp);
//        IContentBuilder contentBuilder = new XmlContentBuilder();

//        var responseMessage = await httpsClientHelper
//            .addTimeout(TimeSpan.FromSeconds(30))
//            .SendAsync(url, HttpMethod.Post, body.ToString(), contentBuilder);

//        return Ok(responses);
//    }
        
//    /// <summary>
//    /// Call Simple Http Request a json body
//    /// </summary>
//    /// <param name="testRequest"></param>
//    /// <returns></returns>
//    [HttpGet("withJsonBody")]
//    public async Task<IActionResult> withJsonBody(string url, string content) {
//        List<object> responses = new List<object>();
//        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
//        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
//            loggerExtension<MyRequest>.TraceAsync(
//                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
//                Serilog.Events.LogEventLevel.Information,
//                null,
//                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
//                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
//                );
//            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit, payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult() });
//            return Task.CompletedTask;
//        };
//        actionsHttp.Add(traceRetry);
//        var httpsClientHelper = new httpsClientHelper(actionsHttp);
//        IContentBuilder contentBuilder = new JsonContentBuilder();

//        var responseMessage = await httpsClientHelper
//            .addTimeout(TimeSpan.FromSeconds(30))
//            .SendAsync(url, HttpMethod.Post, content, contentBuilder);

//        return Ok(responses);
//    }

//    /// <summary>
//    /// Call Simple Http Request a json body
//    /// </summary>
//    /// <param name="testRequest"></param>
//    /// <returns></returns>
//    [HttpGet("withFormBody")]
//    public async Task<IActionResult> withFormBody(string url) {
//        List<object> responses = new List<object>();
//        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
//        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
//            loggerExtension<MyRequest>.TraceAsync(
//                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
//                Serilog.Events.LogEventLevel.Information,
//                null,
//                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
//                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
//                );
//            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit, payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult() });
//            return Task.CompletedTask;
//        };
//        actionsHttp.Add(traceRetry);
//        var httpsClientHelper = new httpsClientHelper(actionsHttp);
//        string content = "key1=value1&key2=value2&key3=value3";
//        var form = content.Split("&").Select(x => x.Split("=")).ToDictionary(x => x[0], x => x[1]);
//        IContentBuilder contentBuilder = new FormUrlEncodedContentBuilder();

//        var responseMessage = await httpsClientHelper
//            .addTimeout(TimeSpan.FromSeconds(30))
//            .SendAsync(url, HttpMethod.Post, form, contentBuilder);

//        return Ok(responses);
//    }

//    [HttpPost("testAll")]
//    public async Task<IActionResult> testAll([FromBody] HttpTestRequest testRequest) {
//        string url = testRequest.Url;
//        object contentBody = testRequest.contentBody;
//        string httpmethod = testRequest.HttpMethod;
//        string contentType = testRequest.ContentType;
//        int httpStatusForRetry = testRequest.httpStatusForRetry;
//        int numberRetries = testRequest.numberRetries;
//        int secondsDelayEspOnRetry = testRequest.secondsDelayEspOnRetry;

//        List<object> responses = new List<object>();
//        List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>>();
//        Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> traceRetry = (httpreq, httpres, totRetry, timeSpanRateLimit) => {
//            loggerExtension<MyRequest>.TraceAsync(
//                new MyRequest { Action = "Test Http", IdTransaction = "Alex" },
//                Serilog.Events.LogEventLevel.Information,
//                null,
//                "HTTP LOG: {totRetry} {timeSpanRateLimit} {Url}, {request} {httpStatus} {BodyResponse}",
//                totRetry, timeSpanRateLimit, url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
//                );
//            responses.Add(new { httpStatus = httpres.StatusCode.ToString(), NumberRetry = totRetry, timeSpanRateLimit = timeSpanRateLimit, payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult() });
//            return Task.CompletedTask;
//        };
//        actionsHttp.Add(traceRetry);
//        var httpsClientHelper = new httpsClientHelper(actionsHttp);
//        if (contentType.Equals("application/x-www-form-urlencoded", StringComparison.InvariantCultureIgnoreCase)) {
//            contentBody = contentBody.ToString().TrimStart('?');
//            contentBody = contentBody.ToString().Split("&").Select(x => x.Split("=")).ToDictionary(x => x[0], x => x[1]);
//        }
//        // 2) Seleziona l’IContentBuilder in modo fluente
//        IContentBuilder contentBuilder = (contentBody, contentType) switch {
//            (null or "", _) => new NoBodyContentBuilder(),
//            (_, "application/json") => new JsonContentBuilder(),
//            (_, "application/x-www-form-urlencoded") => new FormUrlEncodedContentBuilder(),
//            (_, "application/xml") => new XmlContentBuilder(),
//            _ => new NoBodyContentBuilder()
//        };

//        httpsClientHelper
//            .addTimeout(TimeSpan.FromSeconds(30))
//            .addRetryCondition((httpRes) => (int)httpRes.StatusCode == httpStatusForRetry, numberRetries, secondsDelayEspOnRetry)
//            .addRateLimitOnMoreRequests(httpClientOptions.FirstOrDefault()?.RateLimitOptions);

//        //Task<HttpResponseMessage> responseMessage = httpsClientHelper.sendAsync(url);
//        for (int i = 0; i < testRequest.totIterations; i++) {
//            Task<HttpResponseMessage> responseMessage = httpsClientHelper.SendAsync(
//                url,
//                httpmethod switch {
//                    "POST" => HttpMethod.Post,
//                    "post" => HttpMethod.Post,
//                    "get" => HttpMethod.Get,
//                    _ => HttpMethod.Get
//                },
//                contentBody,
//                contentBuilder
//                );
//            var responseHttp = responseMessage.GetAwaiter().GetResult();
//            string content = $"{responseHttp.StatusCode}: {responseHttp.Content.ReadAsStringAsync().GetAwaiter().GetResult()}";
//        }

//        return Ok(responses);
//    }
//    public class MyRequest : IRequest {
//        public string IdTransaction { get; set; }

//        public string Action { get; set; }
//    }
//    public class HttpTestRequest {
//        public string Url { get; set; }
//        public object contentBody { get; set; }
//        public string ContentType { get; set; }
//        public string HttpMethod { get; set; }
//        public int totIterations { get; set; }
//        public int httpStatusForRetry { get; set; }
//        public int numberRetries { get; set; }
//        public int secondsDelayEspOnRetry { get; set; }
//    }
//}