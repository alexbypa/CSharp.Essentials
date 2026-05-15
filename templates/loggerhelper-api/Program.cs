using CSharpEssentials.LoggerHelper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoggerHelper(builder.Configuration);

var app = builder.Build();
app.UseLoggerHelper();

app.MapGet("/", () => "LoggerHelper API is running. Check your configured sinks.");

app.Run();
