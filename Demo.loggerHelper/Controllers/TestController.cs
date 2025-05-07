using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;

namespace Demo.loggerHelper.Controllers {
    [ApiController]
    [Route("loggerHelper")]
    public class TestController : Controller {
        [HttpGet()]
        public async Task<IActionResult> Index() {
            loggerExtension<RequestInfo>.TraceAsync(new RequestInfo { Action = "Test", IdTransaction = Guid.NewGuid().ToString() }, Serilog.Events.LogEventLevel.Information, null, "start controller at {time}", DateTime.Now);
            return Ok(new {Test="Ok"});
        }
    }
}
