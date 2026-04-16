using CSharpEssentials.LoggerHelper;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// --- LoggerHelper: 5 lines of fluent config ---
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("TestApp")
    .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning, LogEventLevel.Error, LogEventLevel.Fatal)
    .EnableRequestResponseLogging()
);

var app = builder.Build();
app.UseLoggerHelper();

// Test endpoint that uses ILogger<T> — logs flow through LoggerHelper automatically
app.MapGet("/", (ILogger<Program> logger) => {
    logger.LogInformation("Hello from LoggerHelper v5!");
    logger.LogWarning("This is a warning test");
    logger.LogError("This is an error test");
    return "LoggerHelper v5 is working!";
});

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
