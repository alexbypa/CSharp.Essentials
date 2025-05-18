using CSharpEssentials.LoggerHelper.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers.logger.telemetry;
[ApiController]
[Route("[controller]")]
public class TelemetryMetricsController : ControllerBase {
    [HttpGet("current")]
    public IActionResult GetCurrentSecond() {
        var second = CustomMetrics.CurrentSecond;

        var status = second switch {
            >= 45 => "ALERT",
            >= 30 => "WARNING",
            _ => "OK"
        };

        return Ok(new {
            current_second = new {
                value = second,
                status = status
            }
        });
    }
}
