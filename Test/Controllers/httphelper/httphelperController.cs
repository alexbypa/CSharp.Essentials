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
    [HttpGet(Name = "httptest")]
    public async Task<IActionResult> test(string url, string contentBody, string contentType, string httpmethod, int LengthIteration) {
        List<Func<HttpRequestMessage, HttpResponseMessage, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, Task>>();
        //string url = "https://reqres.in/api/users/2";
        Func<HttpRequestMessage, HttpResponseMessage, Task> traceRetry = (httpreq, httpres) => {
            loggerExtension<MiaRichiesta>.TraceAsync(
                new MiaRichiesta { Action = "Test Http", IdTransaction = "Alex" },
                Serilog.Events.LogEventLevel.Information,
                null,
                "INFO RETRY: {Url}, {httpStatus} {BodyResponse}",
                url, httpres.StatusCode.ToString(), httpres.Content.ReadAsStringAsync()
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
            .addRateLimit(httpClientOptions.FirstOrDefault()?.RateLimitOptions);
        for (int i = 0; i < 10; i++) {
            //Task<HttpResponseMessage> responseMessage = httpsClientHelper.sendAsync(url);
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

        return Ok(Response);
    }
    public class MiaRichiesta : IRequest {
        public string IdTransaction { get; set; }

        public string Action { get; set; }
    }
}