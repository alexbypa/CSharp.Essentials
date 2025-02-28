using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Security;

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

        object contentBody = null;
        if (testRequest.UseJsonSample) {
            contentBody = new { UserName = "Alex", Cell = "3452154524" };
        }

        string httpmethod = testRequest.HttpMethod;
        string contentType = testRequest.ContentType;
        List<Func<HttpRequestMessage, HttpResponseMessage, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, Task>>();
        //string url = "https://reqres.in/api/users/2";
        Func<HttpRequestMessage, HttpResponseMessage, Task> traceRetry = (httpreq, httpres) => {
            loggerExtension<MiaRichiesta>.TraceAsync(
                new MiaRichiesta { Action = "Test Http", IdTransaction = "Alex" },
                Serilog.Events.LogEventLevel.Information,
                null,
                "INFO RETRY: {Url}, {request} {httpStatus} {BodyResponse}",
                url, httpreq, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                );
            return Task.CompletedTask;
        };
        actionsHttp.Add(traceRetry);
        var httpsClientHelper = new httpsClientHelper(actionsHttp);

        // 2) Seleziona l’IContentBuilder in modo fluente
        IContentBuilder contentBuilder = (contentBody, contentType) switch {
            // Se contentBody è nullo o vuoto, non abbiamo body
            (null or "", _) => new NoBodyContentBuilder(),
            // Se c’è un body e il contentType è "application/json"
            (_, "application/json") => new JsonContentBuilder(),
            // Se c’è un body e il contentType è "application/x-www-form-urlencoded"
            (_, "application/x-www-form-urlencoded") => new FormUrlEncodedContentBuilder(),
            // Altrimenti consideriamo un raw content builder
            _ => new NoBodyContentBuilder()//TODO: Implementare XmlContentBuilder
        };
        //TODO: 1 https://reqbin.com/post-online ( ottimo per i test )
        //TODO: 2 Implementare Xml Content Builder
        //TODO: 3 provare tutti gli esempi


        List<string> responses = new List<string>();
        httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .addRetryCondition((httpRes) => httpRes.StatusCode != System.Net.HttpStatusCode.OK, 3, 2)
            .addRateLimitOnMoreRequests(httpClientOptions.FirstOrDefault()?.RateLimitOptions);

        //Task<HttpResponseMessage> responseMessage = httpsClientHelper.sendAsync(url);
        for (int i = 0; i < 5; i++) {
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
            responses.Add(content);
        }

        return Ok(responses);
    }
    public class MiaRichiesta : IRequest {
        public string IdTransaction { get; set; }

        public string Action { get; set; }
    }
    public class HttpTestRequest {
        public string Url { get; set; }
        public bool UseJsonSample { get; set; }
        public bool UseForm { get; set; }
        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
    }
}