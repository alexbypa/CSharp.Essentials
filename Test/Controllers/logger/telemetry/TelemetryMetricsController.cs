using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;

namespace Test.Controllers.logger.telemetry;
[ApiController]
[Route("[controller]")]
public class TelemetryMetricsController : ControllerBase {
    private static readonly Meter Meter = new Meter("LoggerHelper");
    private static readonly Counter<double> CpuUsage = Meter.CreateCounter<double>("cpu_usage");

    [HttpGet("elabora")]
    public IActionResult Elabora() {
        CpuUsage.Add(42.0, KeyValuePair.Create<string, object?>("source", "demo"));
        return Ok("Metric sent");
    }
}
