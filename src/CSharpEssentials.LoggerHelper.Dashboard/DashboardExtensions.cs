using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpEssentials.LoggerHelper.Dashboard;

/// <summary>
/// Extension methods for wiring the LoggerHelper embedded dashboard into an ASP.NET Core application.
///
/// Usage (Program.cs):
///   builder.Services.AddLoggerHelper(builder.Configuration);
///   ...
///   app.MapLoggerHelperDashboard();           // serves at /loggerhelper-dashboard
///   app.MapLoggerHelperDashboard("/my-path"); // custom path
/// </summary>
public static class DashboardExtensions {
    private static readonly DateTime _startTime = DateTime.UtcNow;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Maps the embedded LoggerHelper dashboard at <paramref name="path"/>.
    /// Serves a self-contained HTML page with live diagnostics — zero external dependencies.
    /// </summary>
    public static IEndpointRouteBuilder MapLoggerHelperDashboard(
        this IEndpointRouteBuilder endpoints,
        string path = "/loggerhelper-dashboard") {

        var basePath = path.TrimEnd('/');

        endpoints.MapGet(basePath, (HttpContext ctx) => {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            var html = DashboardHtml.GetPage(basePath);
            return ctx.Response.WriteAsync(html);
        })
        .WithName("LoggerHelper-Dashboard")
        .WithSummary("Embedded LoggerHelper diagnostics dashboard")
        .WithDescription("Serves a self-contained HTML dashboard showing sink health, startup errors, routing configuration, and real-time diagnostics.")
        .ExcludeFromDescription();

        endpoints.MapGet(basePath + "/api/data", (
            ILogErrorStore errorStore,
            ILoadedSinkStore sinkStore,
            LoggerHelperOptions options) => {

            var sinks = sinkStore.GetAll();
            var errors = errorStore.GetAll();
            var uptime = DateTime.UtcNow - _startTime;

            var data = new DashboardData {
                ApplicationName = options.ApplicationName,
                Status = errors.Count == 0 ? "OK" : errors.Count < 10 ? "WARNING" : "CRITICAL",
                Uptime = FormatUptime(uptime),
                ActiveSinks = sinks.Count(s => s.Configured),
                FailedSinks = sinks.Count(s => !s.Configured),
                TotalRoutes = options.Routes.Count,
                ErrorCount = errors.Count,
                MaskingEnabled = options.SensitiveDataMasking.Enabled,
                Sinks = sinks.Select(s => new DashboardSink {
                    Name = s.SinkName,
                    PluginType = s.PluginType,
                    Levels = s.Levels.ToList(),
                    Active = s.Configured
                }).ToList(),
                Errors = errors.Select(e => new DashboardError {
                    Timestamp = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    SinkName = e.SinkName,
                    Message = e.ErrorMessage,
                    StackTrace = e.StackTrace,
                    Context = e.ContextInfo
                }).OrderByDescending(e => e.Timestamp).ToList(),
                Routes = options.Routes.Select(r => new DashboardRoute {
                    Sink = r.Sink,
                    Levels = r.Levels
                }).ToList()
            };

            return Results.Json(data, _jsonOptions);
        })
        .WithName("LoggerHelper-Dashboard-API")
        .ExcludeFromDescription();

        return endpoints;
    }

    private static string FormatUptime(TimeSpan ts) {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
        if (ts.TotalHours >= 1)
            return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}
