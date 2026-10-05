using CSharpEssentials.HttpHelper;
using CSharpEssentials.HttpHelper.HttpMocks;
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Dashboard;
using CSharpEssentials.LoggerHelper.Demo.Endpoints;
using CSharpEssentials.LoggerHelper.MCP;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── LoggerHelper ────────────────────────────────────────────────────────────
// Development → appsettings.LoggerHelper.debug.json if present (gitignored; copy from .debug.example.json for external sinks),
//               otherwise the LoggerHelper section of appsettings.Development.json (Console + File)
// Production  → appsettings.LoggerHelper.json       (Console + File + MSSqlServer + PostgreSQL)
builder.Services.AddLoggerHelper(builder.Configuration);
builder.Services.AddLoggerHelperMcp();   // MCP server: POST /mcp (JSON-RPC 2.0)
builder.Services.AddLoggerHelperDashboard();  // Dashboard: /loggerhelper

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

app.MapLoggerHelperDashboard();  // Dashboard: GET /loggerhelper + GET /loggerhelper/api/status + GET /loggerhelper/sse

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

// Root → Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.UseEndpointDefinitions();

app.Run();
