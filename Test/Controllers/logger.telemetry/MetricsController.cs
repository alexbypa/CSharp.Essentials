//using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
//using Microsoft.AspNetCore.Mvc;

//namespace Test.Controllers.logger.telemetry;

//[ApiController]
//[Route("api/[controller]")]
//public class MetricsController : ControllerBase {
//    private readonly TelemetriesDbContext _db;

//    public MetricsController(TelemetriesDbContext db) {
//        _db = db;
//    }

//    [HttpGet]
//    public IActionResult GetLastMetrics() {
//        var result = _db.Metrics
//            .OrderByDescending(m => m.Timestamp)
//            .Take(50)
//            .Select(m => new {
//                m.Timestamp,
//                Metric = m.Name,
//                m.TraceId,
//                Value = m.Value.ToString("0.00") + (m.Name.Contains("duration") ? "ms" : ""),
//                Tags = m.TagsJson ?? ""
//            })
//            .ToList();

//        return Ok(result);
//    }
//    [HttpGet("server")]
//    public IActionResult GetHttpServerDuration() =>
//    Ok(_db.Metrics
//        .Where(m => m.Name == "http.server.duration")
//        .OrderByDescending(m => m.Timestamp)
//        .Take(50)
//        .ToList());

//}
