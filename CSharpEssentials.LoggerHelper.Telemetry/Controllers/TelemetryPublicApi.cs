using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;

namespace CSharpEssentials.LoggerHelper.Telemetry.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryPublicApiController : ControllerBase {
    private readonly TelemetriesDbContext _db;
    public TelemetryPublicApiController(TelemetriesDbContext db) {
        _db = db;
    }
    //Sfruttare le viste su postgresql per ottenere dati con metriche e webhook !
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics() {
        var metrics = await _db.Metrics
            .OrderByDescending(m => m.Timestamp)
            //.Where(m => m.TraceId == "f9b4725f67ef251892b3b9c39e4b5f2d")
            .Take(250)
            .ToListAsync();
        return Ok(metrics);
    }
    [HttpGet("metrics/http")]
    public async Task<IActionResult> GetHttpMetrics() {
        var result = await _db.ViewHttpMetrics
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
        return Ok(result);
    }

    [HttpGet("traces/{traceId}")]
    public async Task<IActionResult> GetTraces(string traceId) {
        var traces = await _db.TraceEntry
            .Where(t => t.TraceId == traceId)
            .OrderBy(t => t.StartTime)
            .ToListAsync();
        return Ok(traces);
    }

    [HttpGet("logs/{traceId}")]
    public async Task<IActionResult> GetLogs(string traceId) {
        var logs = await _db.LogEntry
            .Where(l => l.IdTransaction == traceId)
            .OrderBy(l => l.raise_date)
            .ToListAsync();
        return Ok(logs);
    }
}
