using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper;
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