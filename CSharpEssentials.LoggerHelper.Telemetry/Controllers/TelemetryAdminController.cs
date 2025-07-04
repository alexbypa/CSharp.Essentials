using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Telemetry.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryAdminController : ControllerBase {
    private readonly IConfiguration _config;
    private readonly TelemetriesDbContext _telemetriesDbContext;
    public TelemetryAdminController(IConfiguration config, TelemetriesDbContext telemetriesDbContext) {
        _config = config;
        _telemetriesDbContext = telemetriesDbContext;
    }
    [HttpPost("setTelemetryEnabled")]
    public IActionResult SetTelemetryEnabled([FromQuery] bool isEnabled) {
        var options = _telemetriesDbContext.LoggerTelemetryOptions.FirstOrDefault();
        if (options == null)
            return BadRequest();
        options.LastUpdated = DateTime.UtcNow;
        options.IsEnabled = isEnabled;
        _telemetriesDbContext.LoggerTelemetryOptions.Update(options); // <--- forza tracking
        _telemetriesDbContext.SaveChanges();
        return Ok(new { IsEnabled = isEnabled });
    }
    [HttpGet("getTelemetryStatus")]
    public IActionResult GetTelemetryStatus() {
        return Ok(new {
            IsEnabled = _telemetriesDbContext.LoggerTelemetryOptions.FirstOrDefault()?.IsEnabled ?? false
        });
    }
}
