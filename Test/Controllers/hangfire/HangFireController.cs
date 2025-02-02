using CSharpEssentials.HangFireHelper;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Test.Controllers.hangfire {
    [ApiController]
    [Route("controller")]
    public class HangFireController : Controller {
        private readonly BackgroundJobHandler jobHandler;
        public HangFireController(BackgroundJobHandler jobHandler) {
            this.jobHandler = jobHandler;
        }
        [HttpPost(Name = "RetryWithHangfire")]
        public async Task<IActionResult> RetryWithHangfire() {
            //loggerExtension.TraceAsync(new Request { Action = ActionRequest.Check, IdTransaction = Guid.NewGuid().ToString() }, Serilog.Events.LogEventLevel.Information, null, "Avvio controller");
            jobHandler.EnqueueWithRetry<HangFireHttpJobRequest>(new HangFireHttpJobRequest {
                Body = JsonSerializer.Serialize(new { test = true }),
                Method = HttpMethod.Post,
                //Url = "https://run.mocky.io/v3/5f80857e-8dc3-4d20-9e5e-a3c1c99e2757",
                Url = "https://run.mocky.io/v3/dd75bd88-98b6-4812-9027-d3e55d7b3441",
                GameSessionID = Guid.NewGuid().ToString(),
                ActionCommand = "ExternalCredit"
            },
                "test",
                Guid.NewGuid().ToString(),
                TimeSpan.FromSeconds(30), 
                1440, 
                0, 
                System.Net.HttpStatusCode.OK, 
                "Status.Validate", 
                0
                /*, 
                (req) => $"Condizione soddisfatta, job completato per comando {req.Action} con chiave {req.IdTransaction}, Risposta ottenuta con successo.",
                (req, totRetry, TotalSeconds, jsonResponse) => $"Retry numero {totRetry} fallito per comando {req.Action} con chiave {req.IdTransaction}. Ritento tra {TotalSeconds} seconds. Risposta ottenuta {jsonResponse}"
                */
            );

            return Ok();
        }
    }
    public class Request {
        public string Action { get; set; }
        public string IdTransaction { get; set; }
    }
}