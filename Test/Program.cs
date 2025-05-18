using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Telemetry;
using CSharpEssentials.HttpHelper;
using Microsoft.OpenApi.Models;
using Serilog.Events;
using System.Text;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using System.Text.Json;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using CSharpEssentials.LoggerHelper.Telemetry.EF.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

#region OpenTelemetry

builder.Services.AddDbContext<MetricsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MetricsDb")));

builder.Services.AddHostedService<MetricsWriterService>();
builder.Services.AddHostedService<OpenTelemetryMeterListenerService>();
#endregion

// Add services to the container.

#region hangFire
// builder.Services.AddHangFire<Request>(builder);
#endregion
#region LoggerHelper
builder.Services.AddloggerConfiguration(builder);
builder.Services.AddLoggerTelemetry();
#endregion
#region httpExtension
builder.Services.AddOptions();
builder.Services.AddHttpClients(builder.Configuration);
#endregion
builder.Services.AddHttpClient();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LoggerHelper Test", Version = "v1" });

    c.MapType<LogEventLevel>(() => new OpenApiSchema {
        Type = "string",
        Enum = Enum.GetNames(typeof(LogEventLevel))
            .Select(name => new Microsoft.OpenApi.Any.OpenApiString(name))
            .Cast<Microsoft.OpenApi.Any.IOpenApiAny>()
            .ToList()
    });
});

var app = builder.Build();
app.UseStaticFiles(); // << deve essere PRIMA di app.UseRouting()

#region hangFire
// app.UseCustomHangfireDashboard(builder.Configuration);
#endregion
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}
#region loggerExtension
app.UseMiddleware<RequestResponseLoggingMiddleware>();
#endregion
app.UseAuthorization();

app.MapControllers();


#region OpenTelemetry

const string traceFile = "traces.json";

// Endpoint OTLP - /v1/traces
app.MapPost("/v1/traces", async (HttpRequest request) => {
    try {
        using var memory = new MemoryStream();
        await request.Body.CopyToAsync(memory);
        memory.Position = 0;

        var exportRequest = ExportTraceServiceRequest.Parser.ParseFrom(memory);

        // Serializzazione semplice senza custom converter
        var json = System.Text.Json.JsonSerializer.Serialize(exportRequest, new System.Text.Json.JsonSerializerOptions {
            WriteIndented = false,
            PropertyNamingPolicy = null
        });

        await File.AppendAllTextAsync(traceFile, json + ",\n", Encoding.UTF8);
        return Results.Ok();
    } catch (Exception ex) {
        Console.WriteLine("Protobuf parsing error:");
        Console.WriteLine(ex.ToString());
        return Results.BadRequest($"Errore nel parsing Protobuf (traces): {ex.Message}");
    }
});

// Serve una dashboard HTML (traces)
app.MapGet("/dashboard", async context => {
    var html = await File.ReadAllTextAsync("dashboard.html");
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
});

// Serve il file JSON - traces
app.MapGet("/data/traces", async context => {
    context.Response.ContentType = "application/json";
    if (File.Exists(traceFile)) {
        var content = await File.ReadAllTextAsync(traceFile);
        var cleaned = content.TrimEnd(',', '\n');
        await context.Response.WriteAsync("[" + cleaned + "]");
    } else {
        await context.Response.WriteAsync("[]");
    }
});

const string metricsFile = "metrics.json";

app.MapPost("/v1/metrics", async (HttpRequest request) => {
    try {
        //using var memory = new MemoryStream();
        //await request.Body.CopyToAsync(memory);
        //memory.Position = 0;

        var allMetrics = new List<ExportMetricsServiceRequest>();

        // deserializza la richiesta attuale
        var exportRequest = ExportMetricsServiceRequest.Parser.ParseFrom(request.Body);
        allMetrics.Add(exportRequest);

        // salvalo tutto come array
        var options = new JsonSerializerOptions { WriteIndented = false };
        var jsonArray = JsonSerializer.Serialize(allMetrics, options);
        await File.WriteAllTextAsync(metricsFile, jsonArray);

        return Results.Ok();
    } catch (Exception ex) {
        Console.WriteLine("Protobuf parsing error (metrics):");
        Console.WriteLine(ex.ToString());
        return Results.BadRequest($"Errore nel parsing Protobuf (metrics): {ex.Message}");
    }
});
app.MapGet("/data/metrics", async context => {
    context.Response.ContentType = "application/json";
    if (File.Exists(metricsFile)) {
        var content = await File.ReadAllTextAsync(metricsFile);
        await context.Response.WriteAsync(content);
    } else {
        await context.Response.WriteAsync("[]");
    }
});

#endregion

app.Run();
