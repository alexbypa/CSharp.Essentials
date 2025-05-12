using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Test.Controllers.logger {
    [ApiController]
    [Route("loggerHelper")]
    public class LoggerController : Controller {
        private readonly HttpClient _httpClient;
        private Request _request;
        public LoggerController(IHttpClientFactory httpClientFactory) {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://reqres.in/");
            _request = new Request { IdTransaction = Guid.NewGuid().ToString() };
        }
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(int page) {
            var response = await _httpClient.GetAsync($"api/users?page={page}");
            var content = await response.Content.ReadAsStringAsync();
            _request.Action = "GetUsers";
            loggerExtension<Request>.TraceAsync(_request,  Serilog.Events.LogEventLevel.Information, null, "send request on users page # {page}", 2);
            return Content(content, "application/json");
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser() {
            var payload = new {
                name = "morpheus",
                job = "leader"
            };
            _request.Action = "CreateUser";
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/users", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            loggerExtension<Request>.TraceAsync(_request, Serilog.Events.LogEventLevel.Information, null, "send request on page CreateUser");
            return Content(responseContent, "application/json");
        }

        [HttpGet("usernotfound")]
        public async Task<IActionResult> GetUserNotFound() {
            _request.Action = "GetUserNotFound";
            var response = await _httpClient.GetAsync("api/users/23");

            if (!response.IsSuccessStatusCode) {
                return StatusCode((int)response.StatusCode, "User not found");
            }

            var content = await response.Content.ReadAsStringAsync();
            loggerExtension<Request>.TraceAsync(_request, Serilog.Events.LogEventLevel.Information, null, "send request on page usernotfound");
            return Content(content, "application/json");
        }
        [HttpGet(Name = "Info")]
        public async Task<IActionResult> Info(string action, string message, Serilog.Events.LogEventLevel level) {
            loggerExtension<Request>.TraceAsync(new Request { Action = action, IdTransaction = Guid.NewGuid().ToString() }, level, null, message);
            return Ok();
        }
    }
    class Request : IRequest {
        public string IdTransaction { get; set; }
        public string Action { get; set; }
        public string ApplicationName { get; set; } = "LoggerHelper";
    }
}