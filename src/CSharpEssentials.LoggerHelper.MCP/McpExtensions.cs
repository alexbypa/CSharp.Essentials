using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// Maps the MCP JSON-RPC 2.0 endpoint at <paramref name="path"/>.
    /// Call this after <c>app.UseLoggerHelper()</c>.
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
        .WithSummary("Model Context Protocol endpoint (JSON-RPC 2.0)")
        .WithDescription(
            "Exposes LoggerHelper diagnostics to any MCP-compatible AI client. " +
            "Supported methods: initialize, tools/list, tools/call. " +
            "Available tools: loggerhelper_get_health, loggerhelper_get_errors, " +
            "loggerhelper_get_sinks, loggerhelper_get_config.");

        return endpoints;
    }

    private static McpResponse Dispatch(McpRequest request, LoggerHelperMcpTools tools) =>
        request.Method switch {
            "initialize" => new McpResponse {
                Id     = request.Id,
                Result = new {
                    protocolVersion = "2024-11-05",
                    capabilities    = new { tools = new { } },
                    serverInfo      = new { name = "CSharpEssentials.LoggerHelper.MCP", version = "5.0.9" }
                }
            },
            "tools/list" => new McpResponse {
                Id     = request.Id,
                Result = new { tools = BuildToolList() }
            },
            "tools/call" => CallTool(request, tools),
            _ => new McpResponse {
                Id    = request.Id,
                Error = new McpError { Code = -32601, Message = $"Method not found: {request.Method}" }
            }
        };

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
