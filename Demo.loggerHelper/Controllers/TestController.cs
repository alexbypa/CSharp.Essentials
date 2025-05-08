using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;

namespace Demo.loggerHelper.Controllers {
    [ApiController]
    [Route("loggerHelper")]
    public class TestController : Controller {
        [HttpGet("Info")]
        public async Task<IActionResult> Info() {
            loggerExtension<RequestInfo>.TraceAsync(new RequestInfo { Action = "Test", IdTransaction = Guid.NewGuid().ToString() }, Serilog.Events.LogEventLevel.Information, null, "start controller at {time}", DateTime.Now);
            return Ok(new {Test="Ok"});
        }
        [HttpGet("Error")]
        public async Task<IActionResult> Error() {
            loggerExtension<RequestInfo>.TraceAsync(new RequestInfo { Action = "Test", IdTransaction = Guid.NewGuid().ToString() }, Serilog.Events.LogEventLevel.Error, new Exception("Sample exception test"), "start controller at {time}", DateTime.Now);
            return Ok(new {Test="Ok"});
        }
    }
}
