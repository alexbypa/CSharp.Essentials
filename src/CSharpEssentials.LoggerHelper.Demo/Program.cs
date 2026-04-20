using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Demo.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// LoggerHelper — configurazione da appsettings.LoggerHelper.json
builder.Services.AddLoggerHelper(builder.Configuration);

// Endpoint registration (SOLID — ogni endpoint in un file separato)
builder.Services.AddSingleton<IEndpointDefinition, BasicLoggingEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, TraceApiEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, CustomPropertiesEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, RoutingDemoEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, DiagnosticsEndpoints>();

var app = builder.Build();

// Request/Response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseEndpointDefinitions();

app.Run();
