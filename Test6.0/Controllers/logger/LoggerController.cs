using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers.logger;
[ApiController]
[Route("loggerHelper")]
public class LoggerController : Controller {
    private readonly HttpClient _httpClient;
    private Request _request;
    int _page = 2;
    public LoggerController(IHttpClientFactory httpClientFactory) {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("https://reqres.in/");
        //ApplicationName is on appSettings.json
        _request = new Request { IdTransaction = Guid.NewGuid().ToString(), Action = "Users" };
    }
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() {
        _request.Action = "GetUsers";
        int page = await UserService_getPage();
        string content = await UserService_CallPage();
        await UserService_SaveResponse();
        return Content(content, "application/json");
    }
    private async Task<int> UserService_getPage() {
        //Simultate load data from DB
        await Task.Delay(300);
        int page = _page;
        loggerExtension<Request>.TraceAsync(_request, Serilog.Events.LogEventLevel.Information, null, "Search page on DB# {page}", page);
        return 2;
    }
    private async Task<int> UserService_SaveResponse() {
        //Save on DB
        await Task.Delay(300);
        loggerExtension<Request>.TraceAsync(_request, Serilog.Events.LogEventLevel.Information, null, "Save result on DB # {page}", _page);
        return 2;
    }
    private async Task<string> UserService_CallPage() {
        //Save on DB
        await Task.Delay(300);
        int page = _page;
        var response = await _httpClient.GetAsync($"api/users?page={page}");
        var content = await response.Content.ReadAsStringAsync();
        loggerExtension<Request>.TraceAsync(_request, Serilog.Events.LogEventLevel.Information, null, "Call http on page # {page}", page);
        return content;
    }
}
class Request : IRequest {
    public string IdTransaction { get; set; }
    public string Action { get; set; }
    public string ApplicationName { get; set; }
}
