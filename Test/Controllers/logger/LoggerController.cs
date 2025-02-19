using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers.logger {
    [ApiController]
    [Route("controller")]
    public class LoggerController : Controller {
        [HttpGet(Name = "test")]
        public async Task<IActionResult> test() {
            loggerExtension<Request>.TraceAsync(new Request{ Action = "Prova", IdTransaction = Guid.NewGuid().ToString() }, Serilog.Events.LogEventLevel.Information, null, "Avvio controller alle ore {time}", DateTime.Now);
            return Ok();
        }
    }

    class Request : IRequest {
        public string Action { get; set; }
        public string IdTransaction { get; set; }
    }
    interface IRequest {
        string Action { get; set; }
        string IdTransaction { get; set; }
    }

}
