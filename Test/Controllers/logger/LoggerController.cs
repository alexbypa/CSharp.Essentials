using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers.logger {
    [ApiController]
    [Route("controller")]
    public class LoggerController : Controller {
        [HttpGet(Name = "test")]
        public async Task<IActionResult> test() {
            loggerExtension<Request>.TraceAsync(new Request{ Action = "Prova", IdTransaction = "asdad" }, Serilog.Events.LogEventLevel.Information, null, "Avvio controller alle ore {time}", DateTime.Now);
            return Ok();
        }
    }
    class Request : IRequest {
        public string IdTransaction { get; set; }
        public string Action { get; set; }
    }
}
