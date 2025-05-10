using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers.logger {
    [ApiController]
    [Route("loggerHelper")]
    public class LoggerController : Controller {
        [HttpGet(Name = "Info")]
        public async Task<IActionResult> Info(string action, string message, Serilog.Events.LogEventLevel level) {
            loggerExtension<Request>.TraceAsync(new Request{ Action = action, IdTransaction = Guid.NewGuid().ToString() }, level, null, message);
            return Ok();
        }
    }
    class Request : IRequest {
        public string IdTransaction { get; set; }
        public string Action { get; set; }
        public string ApplicationName { get; set; } = "LoggerHelper";
    }
}