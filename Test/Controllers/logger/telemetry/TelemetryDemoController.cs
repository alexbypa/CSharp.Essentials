using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Serilog.Events;
using CSharpEssentials.LoggerHelper;

namespace LoggerHelper.Demo.Controllers;

[ApiController]
[Route("[controller]")]
public class TelemetryDemoController : ControllerBase {
    private static readonly ActivitySource ActivitySource = new("LoggerHelper");

    [HttpGet("elabora")]
    public async Task<IActionResult> Elabora() {
        using var activity = ActivitySource.StartActivity("ElaboraRequest");
        
        var request = new DemoRequest();
        if (Activity.Current != null) 
            request.IdTransaction = Activity.Current?.TraceId.ToString();

        activity?.SetTag("utente", "mario.rossi");
        //activity?.SetTag("cpu_usage", GetCpuUsage());
        activity?.SetTag("memoria_mb", GC.GetTotalMemory(false) / 1024 / 1024);
        activity?.SetTag("db_status", "ok");



        loggerExtension<DemoRequest>.TraceAsync(
            request,
            LogEventLevel.Information,
            null,
             "Start Demo"
            );

        // Simula un'elaborazione di 300ms
        await Task.Delay(300);

        loggerExtension<DemoRequest>.TraceAsync(
            request,
            LogEventLevel.Information,
            null,
             "Interrogo il DB"
            );

        await Task.Delay(200);
        loggerExtension<DemoRequest>.TraceAsync(
            request,
            LogEventLevel.Information,
            null,
            "Finish Demo with success"
            );

        return Ok("Action completed");
    }
}
public class DemoRequest : IRequest {
    public string IdTransaction { get; set; } = Guid.NewGuid().ToString();
    public string Action => "Test";
    public string ApplicationName => "Demo Telemetry";
}