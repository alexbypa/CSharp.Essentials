using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace CSharpEssentials.LoggerHelper.MCP;

/// <summary>
/// Extension methods for wiring LoggerHelper's MCP server into an ASP.NET Core application.
///
/// Usage (Program.cs):
///   builder.Services.AddLoggerHelper(builder.Configuration);
///   builder.Services.AddLoggerHelperMcp();
///   ...
///   app.MapLoggerHelperMcp("/mcp");
///
/// Then call from any MCP client (Claude, Cursor, Copilot, curl):
///   POST /mcp  {"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}
/// </summary>
public static class McpExtensions {
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented          = false
    };

    /// <summary>
    /// Registers <see cref="LoggerHelperMcpTools"/> in the DI container.
    /// Call this before <c>builder.Build()</c>.
    /// </summary>
    public static IServiceCollection AddLoggerHelperMcp(this IServiceCollection services) {
        services.AddTransient<LoggerHelperMcpTools>();
        return services;
    }

    /// <summary>
    /// Maps the MCP JSON-RPC 2.0 endpoint at <paramref name="path"/> (Streamable HTTP transport).
    /// Call this after <c>app.UseLoggerHelper()</c>.
    /// Supported methods: <c>initialize</c>, <c>tools/list</c>, <c>tools/call</c>,
    /// <c>prompts/list</c>, <c>prompts/get</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapLoggerHelperMcp(
        this IEndpointRouteBuilder endpoints,
        string path = "/mcp") {

        endpoints.MapPost(path, async (HttpContext ctx, LoggerHelperMcpTools tools, CancellationToken ct) => {
            McpRequest? request = null;
            try {
                request = await JsonSerializer.DeserializeAsync<McpRequest>(
                    ctx.Request.Body, _jsonOptions, ct);
            } catch { /* malformed JSON — request stays null, returns parse error below */ }

            var response = request is null
                ? new McpResponse { Error = new McpError { Code = -32700, Message = "Parse error" } }
                : Dispatch(request, tools);

            ctx.Response.ContentType = "application/json; charset=utf-8";
            await JsonSerializer.SerializeAsync(ctx.Response.Body, response, _jsonOptions, ct);
        })
        .WithName("LoggerHelper-MCP")
        .WithSummary("Model Context Protocol endpoint (JSON-RPC 2.0 — Streamable HTTP)")
        .WithDescription(
            "Exposes LoggerHelper diagnostics to any MCP-compatible AI client. " +
            "Supported methods: initialize, tools/list, tools/call, prompts/list, prompts/get. " +
            "Tools: loggerhelper_get_health, loggerhelper_get_errors, loggerhelper_get_sinks, loggerhelper_get_config. " +
            "Prompts: diagnose-logging (focus: health|errors|sinks|config|all). " +
            "For Claude Desktop / SSE clients use GET /mcp/sse + POST /mcp/messages instead.");

        return endpoints;
    }

    /// <summary>
    /// Maps the MCP HTTP+SSE transport (MCP spec 2024-11-05) for clients such as Claude Desktop
    /// and the MCP Inspector that require a persistent SSE stream.
    /// <para>
    /// Usage — Program.cs:<br/>
    /// <c>app.MapLoggerHelperMcpSse();</c>  // defaults: GET /mcp/sse, POST /mcp/messages
    /// </para>
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="ssePath">SSE stream endpoint (default: <c>/mcp/sse</c>).</param>
    /// <param name="messagesPath">JSON-RPC message receiver (default: <c>/mcp/messages</c>).</param>
    public static IEndpointRouteBuilder MapLoggerHelperMcpSse(
        this IEndpointRouteBuilder endpoints,
        string ssePath      = "/mcp/sse",
        string messagesPath = "/mcp/messages") {

        var sessions = new ConcurrentDictionary<string, Channel<string>>();

        endpoints.MapGet(ssePath, async (HttpContext ctx, CancellationToken ct) => {
            var sessionId = Guid.NewGuid().ToString("N")[..12];
            var channel   = Channel.CreateUnbounded<string>(new UnboundedChannelOptions {
                SingleReader                  = true,
                AllowSynchronousContinuations = false
            });
            sessions[sessionId] = channel;

            ctx.Response.ContentType                  = "text/event-stream; charset=utf-8";
            ctx.Response.Headers["Cache-Control"]     = "no-cache";
            ctx.Response.Headers["X-Accel-Buffering"] = "no";

            try {
                await ctx.Response.WriteAsync(
                    $"event: endpoint\ndata: {messagesPath}?sessionId={sessionId}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);

                await foreach (var json in channel.Reader.ReadAllAsync(ct)) {
                    await ctx.Response.WriteAsync($"event: message\ndata: {json}\n\n", ct);
                    await ctx.Response.Body.FlushAsync(ct);
                }
            } catch (OperationCanceledException) { /* client disconnected — normal exit */ }
            finally {
                sessions.TryRemove(sessionId, out _);
                channel.Writer.TryComplete();
            }
        })
        .WithName("LoggerHelper-MCP-SSE")
        .WithSummary("MCP SSE transport — persistent event stream (2024-11-05)")
        .WithDescription(
            "Opens a persistent SSE stream for MCP clients that use the 2024-11-05 HTTP+SSE transport " +
            "(Claude Desktop, MCP Inspector). On connect the server emits 'event: endpoint' with the URL " +
            "to POST messages to. JSON-RPC responses are pushed back as 'event: message' SSE events.");

        endpoints.MapPost(messagesPath, async (HttpContext ctx, LoggerHelperMcpTools tools, CancellationToken ct) => {
            var sessionId = ctx.Request.Query["sessionId"].ToString();
            if (!sessions.TryGetValue(sessionId, out var channel)) {
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsync(
                    $"Session '{sessionId}' not found or expired. Re-connect to {ssePath}.", ct);
                return;
            }

            McpRequest? request = null;
            try {
                request = await JsonSerializer.DeserializeAsync<McpRequest>(
                    ctx.Request.Body, _jsonOptions, ct);
            } catch { }

            var response = request is null
                ? new McpResponse { Error = new McpError { Code = -32700, Message = "Parse error" } }
                : Dispatch(request, tools);

            await channel.Writer.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions), ct);
            ctx.Response.StatusCode = 202;
        })
        .WithName("LoggerHelper-MCP-Messages")
        .WithSummary("MCP SSE messages — POST JSON-RPC requests to the active SSE session")
        .WithDescription(
            "Accepts a JSON-RPC 2.0 request, dispatches it, and delivers the response over the SSE " +
            "stream identified by 'sessionId'. Returns HTTP 202 Accepted; the response arrives as " +
            "'event: message' on the SSE connection.");

        return endpoints;
    }

    private static McpResponse Dispatch(McpRequest request, LoggerHelperMcpTools tools) =>
        request.Method switch {
            "initialize" => new McpResponse {
                Id     = request.Id,
                Result = new {
                    protocolVersion = "2024-11-05",
                    capabilities    = new { tools = new { }, prompts = new { } },
                    serverInfo      = new { name = "CSharpEssentials.LoggerHelper.MCP", version = "5.0.9" }
                }
            },
            "tools/list"   => new McpResponse {
                Id     = request.Id,
                Result = new { tools = BuildToolList() }
            },
            "tools/call"   => CallTool(request, tools),
            "prompts/list" => new McpResponse {
                Id     = request.Id,
                Result = new { prompts = LoggerHelperMcpPrompts.BuildList() }
            },
            "prompts/get"  => GetPrompt(request),
            _ => new McpResponse {
                Id    = request.Id,
                Error = new McpError { Code = -32601, Message = $"Method not found: {request.Method}" }
            }
        };

    private static McpResponse GetPrompt(McpRequest request) {
        var name  = "";
        var focus = "all";

        if (request.Params is { ValueKind: JsonValueKind.Object } p) {
            if (p.TryGetProperty("name", out var n))
                name = n.GetString() ?? "";
            if (p.TryGetProperty("arguments", out var args)
                && args.ValueKind == JsonValueKind.Object
                && args.TryGetProperty("focus", out var f))
                focus = f.GetString() ?? "all";
        }

        try {
            return new McpResponse { Id = request.Id, Result = LoggerHelperMcpPrompts.Get(name, focus) };
        } catch (Exception ex) {
            return new McpResponse {
                Id    = request.Id,
                Error = new McpError { Code = -32602, Message = ex.Message }
            };
        }
    }

    private static McpResponse CallTool(McpRequest request, LoggerHelperMcpTools tools) {
        var toolName  = "";
        JsonElement? args = null;

        if (request.Params is { ValueKind: JsonValueKind.Object } p) {
            if (p.TryGetProperty("name", out var nameProp))
                toolName = nameProp.GetString() ?? "";
            if (p.TryGetProperty("arguments", out var argsProp))
                args = argsProp;
        }

        string text;
        try {
            text = toolName switch {
                "loggerhelper_get_errors" => tools.GetErrors(GetInt(args, "count", 20)),
                "loggerhelper_get_sinks"  => tools.GetSinks(),
                "loggerhelper_get_config" => tools.GetConfig(),
                "loggerhelper_get_health" => tools.GetHealth(),
                _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
            };
        } catch (Exception ex) {
            return new McpResponse {
                Id    = request.Id,
                Error = new McpError { Code = -32603, Message = ex.Message }
            };
        }

        return new McpResponse {
            Id     = request.Id,
            Result = new { content = new[] { new { type = "text", text } } }
        };
    }

    private static McpToolDefinition[] BuildToolList() => new[] {
        new McpToolDefinition {
            Name        = "loggerhelper_get_health",
            Description = "Returns the overall health status (OK / WARNING / CRITICAL), active sink count, and recent error count.",
            InputSchema = new { type = "object", properties = new { } }
        },
        new McpToolDefinition {
            Name        = "loggerhelper_get_errors",
            Description = "Returns recent LoggerHelper sink errors (failed writes, misconfiguration, network issues).",
            InputSchema = new {
                type       = "object",
                properties = new {
                    count = new { type = "integer", description = "Number of recent errors to return (default: 20)" }
                }
            }
        },
        new McpToolDefinition {
            Name        = "loggerhelper_get_sinks",
            Description = "Returns all configured LoggerHelper sink routes with their ACTIVE/FAILED status and assigned log levels.",
            InputSchema = new { type = "object", properties = new { } }
        },
        new McpToolDefinition {
            Name        = "loggerhelper_get_config",
            Description = "Returns the current LoggerHelper configuration: application name, routing rules, and sensitive data masking settings.",
            InputSchema = new { type = "object", properties = new { } }
        }
    };

    private static int GetInt(JsonElement? el, string key, int fallback) {
        if (el is { ValueKind: JsonValueKind.Object } j
            && j.TryGetProperty(key, out var prop)
            && prop.TryGetInt32(out var val))
            return val;
        return fallback;
    }
}
