using CSharpEssentials.LoggerHelper;
using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers.logger {
    [ApiController]
    [Route("loggerHelper")]
    public class LoggerController : Controller {
        [HttpGet(Name = "loggertest")]
        public async Task<IActionResult> test() {
            //TODO: non devo usare chiavi segrete o connessioni a DB
            //TODO: Aggiungere e spiegare l' uso di Serilog.Debugging.SelfLog
            //Scrivere sul readme la rimozione 🔧 WriteTo ( per non scrivere su sinks non configurati )  


            loggerExtension<Request>.TraceAsync(new Request{ Action = "Prova", IdTransaction = "asdad" }, Serilog.Events.LogEventLevel.Information, null, "Avvio controller alle ore {time} ApplicationName: {ApplicationName}", DateTime.Now, "HubGame");
            return Ok();
        }
    }
    class Request : IRequest {
        public string IdTransaction { get; set; }
        public string Action { get; set; }
    }
}
