using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Dashboard;
using CSharpEssentials.LoggerHelper.Demo.Endpoints;
using CSharpEssentials.LoggerHelper.MCP;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── LoggerHelper ────────────────────────────────────────────────────────────
// Development → appsettings.LoggerHelper.debug.json (Console + File, no DB deps)
// Production  → appsettings.LoggerHelper.json       (Console + File + MSSqlServer + PostgreSQL)
builder.Services.AddLoggerHelper(builder.Configuration);
builder.Services.AddLoggerHelperMcp();        // MCP server: POST /mcp (JSON-RPC 2.0)
builder.Services.AddLoggerHelperDashboard();  // Dashboard: /loggerhelper

// ── Endpoint modules ────────────────────────────────────────────────────────
builder.Services.AddSingleton<IEndpointDefinition, BasicLoggingEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, ContextualLoggingEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, TraceApiEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, CustomPropertiesEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, RoutingDemoEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, DiagnosticsEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, DynamicFileEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, SensitiveDataMaskingEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, McpDemoEndpoints>();

// ── Swagger ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title       = "CSharpEssentials.LoggerHelper — Demo",
        Version     = "v5",
        Description = """
            Interactive demo for CSharpEssentials.LoggerHelper.
            Each endpoint triggers a different logging scenario — hit an endpoint,
            then check the console and Logs/ to see structured output in real time.

            Run with:  dotnet run --project src/CSharpEssentials.LoggerHelper.Demo
            Docs:      https://www.loggerhelper.com
            """,
        Contact = new OpenApiContact {
            Name = "Alessandro Chiodo",
            Url  = new Uri("https://github.com/alexbypa/CSharp.Essentials")
        }
    });
});

var app = builder.Build();

// ── Middleware ──────────────────────────────────────────────────────────────
app.UseLoggerHelper();                 // request/response logging + correlation ID
app.MapLoggerHelperMcp("/mcp");        // MCP server for AI assistant tool calls
app.MapLoggerHelperDashboard();        // Dashboard UI at /loggerhelper
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LoggerHelper Demo v5");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});

// Root → Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.UseEndpointDefinitions();

app.Run();
