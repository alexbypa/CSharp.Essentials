using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
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
    public async Task<IActionResult> GetTracesAsync() {
        var now = DateTime.UtcNow;
        var MinutesAgo = now.AddMinutes(-60);

        var traces = await _db.Set<TraceEntry>()
            .Where(t => t.StartTime >= MinutesAgo)
            .Select(t => new {
                traceId = t.TraceId,
                operation = t.Name,
                timestamp = t.StartTime,
                durationMs = t.DurationMs
            })
            .OrderByDescending(t => t.timestamp)
            .ToListAsync();

        return Ok(traces);
    }
    /// <summary>
    /// Return traces by id
    /// </summary>
    /// <param name="traceId"></param>
    /// <returns></returns>
    [HttpGet("traces/{traceId}")]
    public async Task<IActionResult> GetTraceById(string traceId) {
        try {
            var traceDb = await _db.Set<TraceEntry>()
                .Where(t => t.TraceId == traceId)
                .FirstOrDefaultAsync();

            if (traceDb == null)
                return NotFound();

            // parsing lato C#, non in LINQ-to-SQL
            var trace = new {
                traceDb.TraceId,
                traceDb.Name,
                traceDb.StartTime,
                traceDb.EndTime,
                traceDb.DurationMs,
                Tags = string.IsNullOrEmpty(traceDb.TagsJson) ? "{}" : traceDb.TagsJson
            };

            return Ok(trace);
        } catch (Exception ex) {
            Console.WriteLine(ex.ToString());
        }
        return Ok("");
    }
    /// <summary>
    /// get Spans by TraceId
    /// </summary>
    /// <param name="traceId"></param>
    /// <returns></returns>
    [HttpGet("traces/{traceId}/spans")]
    public async Task<IActionResult> GetSpansByTraceId(string traceId) {
        try {
            var spansDb = await _db.Set<TraceEntry>()
                .Where(t => t.TraceId == traceId)
                .ToListAsync();

            var spans = spansDb.Select(t => new
            {
                t.TraceId,
                t.Name,
                t.StartTime,
                t.EndTime,
                t.DurationMs,
                Tags = string.IsNullOrEmpty(t.TagsJson) ? "{}" : t.TagsJson
            });

            return Ok(spans);
        } catch (Exception ex) {
            Console.WriteLine(ex.ToString());
            return Problem("Errore durante il recupero degli span.");
        }
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
