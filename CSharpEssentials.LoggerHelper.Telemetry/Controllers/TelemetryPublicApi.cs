using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSharpEssentials.LoggerHelper.Telemetry.Controllers;
/// <summary>
/// Controller to return data for dashboard
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TelemetryPublicApiController : ControllerBase {
    private readonly TelemetriesDbContext _db;
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="db"></param>
    public TelemetryPublicApiController(TelemetriesDbContext db) {
        _db = db;
    }
    /// <summary>
    /// return data for metrics
    /// </summary>
    /// <returns></returns>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics() {
        var metrics = await _db.Metrics
            .OrderByDescending(m => m.Timestamp)
            //.Where(m => m.TraceId == "f9b4725f67ef251892b3b9c39e4b5f2d")
            .Take(250)
            .ToListAsync();
        return Ok(metrics);
    }
    /// <summary>
    /// return data for http metrics
    /// </summary>
    /// <returns></returns>
    [HttpGet("metrics/http")]
    public async Task<IActionResult> GetHttpMetrics() {
        var result = await _db.ViewHttpMetrics
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
        return Ok(result);
    }
    /// <summary>
    /// return data for dns metrics
    /// </summary>
    /// <returns></returns>
    [HttpGet("metrics/dns")]
    public async Task<IActionResult> GetDnsMetrics() {
        var result = await _db.Metrics
            .Where(m => m.Name == "dns.lookup.duration")
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();

        return Ok(result);
    }
    /// <summary>
    /// return data for traces and spans
    /// </summary>
    /// <param name="traceId"></param>
    /// <returns></returns>
    [HttpGet("traces")]
    public async Task<IActionResult> GetTraces() {
        var traces = Enumerable.Range(1, 10).Select(i => new {
            traceId = Guid.NewGuid().ToString(),
            operation = "POST /users/register",
            timestamp = DateTime.UtcNow.AddMinutes(-i)
        });
        return Ok(traces);
    }
    /// <summary>
    /// return data for logs
    /// </summary>
    /// <param name="traceId"></param>
    /// <returns></returns>
    [HttpGet("logs/{traceId}")]
    public async Task<IActionResult> GetLogs(string traceId) {
        var logs = await _db.LogEntry
            .Where(l => l.IdTransaction == traceId)
            .OrderBy(l => l.raise_date)
            .ToListAsync();
        return Ok(logs);
    }
}
