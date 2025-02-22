using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers.httphelper;
[ApiController]
[Route("httpHelper")]
public class httphelperController : Controller {
    [HttpGet(Name = "httptest")]
    public async Task<IActionResult> test() {
        List<Func<HttpRequestMessage, HttpResponseMessage, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, Task>>();
        string url = "https://reqres.in/api/users?delay=2";
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
        Task<HttpResponseMessage> responseMessage = httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(30))
            .addRetryCondition((httpRes) => httpRes.StatusCode == System.Net.HttpStatusCode.OK, 3, 2)
            //.addRateLimit() //TODO: Implementare test rate limit
            .sendAsync(url);

        var responseHttp = responseMessage.GetAwaiter().GetResult();
        string jsonContent = responseHttp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        return Ok();
    }
    public class MiaRichiesta : IRequest {
        public string IdTransaction { get; set; }

        public string Action { get; set; }
    }
}