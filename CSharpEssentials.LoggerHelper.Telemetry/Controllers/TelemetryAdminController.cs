using CSharpEssentials.LoggerHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CSharpEssentials.LoggerHelper.Telemetry.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryAdminController : ControllerBase {
    private readonly IConfiguration _config;
    private readonly ILoggerConfigInfo _loggerConfigInfo;
    public TelemetryAdminController(IConfiguration config, ILoggerConfigInfo loggerConfigInfo) {
        _config = config;
        _loggerConfigInfo = loggerConfigInfo;
    }
    [HttpPost("setTelemetryEnabled")]
    public IActionResult SetTelemetryEnabled([FromQuery] bool isEnabled) {
        var filePath = _loggerConfigInfo.fileNameSettings;

        if (!System.IO.File.Exists(filePath))
            return NotFound("Config file not found.");

        var json = System.IO.File.ReadAllText(filePath);
        var jdoc = JsonNode.Parse(json);

        jdoc!["Serilog"]!["SerilogConfiguration"]!["LoggerTelemetryOptions"]!["IsEnabled"] = isEnabled;
        System.IO.File.WriteAllText(filePath, jdoc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return Ok(new { IsEnabled = isEnabled });
    }
    [HttpGet("getTelemetryStatus")]
    public IActionResult GetTelemetryStatus() {
        var isEnabled = _config.GetValue<bool>("Serilog:SerilogConfiguration:LoggerTelemetryOptions:IsEnabled");
        return Ok(new { IsEnabled = isEnabled });
    }
}
