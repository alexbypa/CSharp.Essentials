using CSharpEssentials.HttpHelper;
using CSharpEssentials.HttpHelper.HttpMocks;
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Dashboard;
using CSharpEssentials.LoggerHelper.Demo.Endpoints;
using CSharpEssentials.LoggerHelper.MCP;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── LoggerHelper ────────────────────────────────────────────────────────────
// Development → appsettings.LoggerHelper.debug.json if present (gitignored; copy from .debug.example.json for external sinks),
//               otherwise the LoggerHelper section of appsettings.Development.json (Console + File)
// Production  → appsettings.LoggerHelper.json       (Console + File + MSSqlServer + PostgreSQL)
builder.Services.AddLoggerHelper(builder.Configuration);
builder.Services.AddLoggerHelperMcp();   // MCP server: POST /mcp (JSON-RPC 2.0)
// Dashboard credentials come from user-secrets (never from appsettings.json, never committed):
//   dotnet user-secrets set "Dashboard:Username" "demo"
//   dotnet user-secrets set "Dashboard:Password" "<a strong password>"
// Without them the Demo generates a one-time password and prints it to the console only (not to the log sinks).
var dashboardUser = builder.Configuration["Dashboard:Username"] is { Length: > 0 } user ? user : "demo";
var dashboardPassword = builder.Configuration["Dashboard:Password"];
if (string.IsNullOrWhiteSpace(dashboardPassword)) {
    dashboardPassword = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
    Console.WriteLine($"[Dashboard] Dashboard:Password not set: one-time credentials {dashboardUser} / {dashboardPassword} (set them with dotnet user-secrets to keep them).");
}
builder.Services.AddLoggerHelperDashboard(o => o.UseBasicAuthentication(dashboardUser, dashboardPassword));  // Dashboard: /loggerhelper (Basic auth)

// ── HttpHelper (in-memory mock upstream, no real network) ───────────────────
builder.Services.AddSingleton<IHttpMockScenario>(HttpHelperEndpoints.FlakyUpstream);
builder.Services.AddHttpClients(builder.Configuration);


// ── Endpoint modules ────────────────────────────────────────────────────────
builder.Services.AddSingleton<IEndpointDefinition, BasicLoggingEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, TraceApiEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, CustomPropertiesEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, RoutingDemoEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, DiagnosticsEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, DynamicFileEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, SensitiveDataMaskingEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, McpDemoEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, ContextualLoggingEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, HttpHelperEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, PlaygroundEndpoints>();
builder.Services.AddSingleton<IEndpointDefinition, PlaygroundHttpEndpoints>();

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

            HttpHelper:  GET /api/httphelper/retry, then watch the retries live in /loggerhelper.

            Run with:  dotnet run --project src/CSharpEssentials.LoggerHelper.Demo
            Docs:      https://www.loggerhelper.it
            """,
        Contact = new OpenApiContact {
            Name = "Alessandro Chiodo",
            Url  = new Uri("https://github.com/alexbypa/CSharp.Essentials")
        }
    });
});

var app = builder.Build();

app.MapLoggerHelperDashboard();  // Dashboard: GET /loggerhelper + /loggerhelper/api/status + /loggerhelper/api/logs + /loggerhelper/api/stream (all require Basic auth)

// ── Middleware ──────────────────────────────────────────────────────────────
app.UseLoggerHelper();              // request/response logging + correlation ID
app.MapLoggerHelperMcp("/mcp");     // MCP Streamable HTTP — POST /mcp  (Claude Code, Cursor, Copilot)
app.MapLoggerHelperMcpSse();        // MCP HTTP+SSE        — GET /mcp/sse + POST /mcp/messages (Claude Desktop)

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LoggerHelper Demo v5");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});

// Scalar UI on /scalar, reading the same Swashbuckle document as Swagger UI
app.MapScalarApiReference(o => o
    .WithTitle("LoggerHelper Demo")
    .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"));

// Playground page: wwwroot/playground.html (docker/docker-compose.yml for the external sinks)
app.UseStaticFiles();

// Root → Playground
app.MapGet("/", () => Results.Redirect("/playground.html")).ExcludeFromDescription();

app.UseEndpointDefinitions();

app.Run();
