using CSharpEssentials.LoggerHelper.Diagnostics;
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
        services.AddTransient<LoggerHelperMcpTools>(sp => new LoggerHelperMcpTools(
            sp.GetRequiredService<ILogErrorStore>(),
            sp.GetRequiredService<ILoadedSinkStore>(),
            sp.GetRequiredService<LoggerHelperOptions>(),
            sp.GetService<ContextualLogBuffer>()
        ));
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
            "loggerhelper_get_sinks, loggerhelper_get_config, loggerhelper_set_log_level, " +
            "loggerhelper_search_logs, loggerhelper_toggle_sink.");

        return endpoints;
    }

    private static McpResponse Dispatch(McpRequest request, LoggerHelperMcpTools tools) =>
        request.Method switch {
            "initialize" => new McpResponse {
                Id     = request.Id,
                Result = new {
                    protocolVersion = "2024-11-05",
                    capabilities    = new { tools = new { } },
                    serverInfo      = new { name = "CSharpEssentials.LoggerHelper.MCP", version = "5.2.0" }
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
                "loggerhelper_get_errors"    => tools.GetErrors(GetInt(args, "count", 20)),
                "loggerhelper_get_sinks"     => tools.GetSinks(),
                "loggerhelper_get_config"    => tools.GetConfig(),
                "loggerhelper_get_health"    => tools.GetHealth(),
                "loggerhelper_set_log_level" => tools.SetLogLevel(GetString(args, "sink", "")!, GetString(args, "levels", "")!),
                "loggerhelper_search_logs"   => tools.SearchLogs(GetString(args, "query", null), GetString(args, "level", null), GetInt(args, "count", 50)),
                "loggerhelper_toggle_sink"   => tools.ToggleSink(GetString(args, "sink", "")!, GetBool(args, "enabled", true)),
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
        },
        new McpToolDefinition {
            Name        = "loggerhelper_set_log_level",
            Description = "Changes the log level routing for a specific sink at runtime. No restart required.",
            InputSchema = new {
                type       = "object",
                properties = new {
                    sink   = new { type = "string", description = "Sink name (e.g., Console, File, Email)" },
                    levels = new { type = "string", description = "Comma-separated log levels (e.g., 'Error,Fatal' or 'Debug,Information,Warning,Error')" }
                },
                required = new[] { "sink", "levels" }
            }
        },
        new McpToolDefinition {
            Name        = "loggerhelper_search_logs",
            Description = "Searches recent log entries in the in-memory contextual ring buffer. Filter by text query and/or log level.",
            InputSchema = new {
                type       = "object",
                properties = new {
                    query = new { type = "string", description = "Text to search for in log messages (case-insensitive)" },
                    level = new { type = "string", description = "Filter by log level (Verbose, Debug, Information, Warning, Error, Fatal)" },
                    count = new { type = "integer", description = "Maximum number of entries to return (default: 50)" }
                }
            }
        },
        new McpToolDefinition {
            Name        = "loggerhelper_toggle_sink",
            Description = "Enables or disables a sink at runtime without application restart. Disabled sinks stop receiving log events; re-enabling restores previous levels.",
            InputSchema = new {
                type       = "object",
                properties = new {
                    sink    = new { type = "string", description = "Sink name to toggle (e.g., Console, Email, Telegram)" },
                    enabled = new { type = "boolean", description = "true to enable, false to disable" }
                },
                required = new[] { "sink", "enabled" }
            }
        }
    };

    private static int GetInt(JsonElement? el, string key, int fallback) {
        if (el is { ValueKind: JsonValueKind.Object } j
            && j.TryGetProperty(key, out var prop)
            && prop.TryGetInt32(out var val))
            return val;
        return fallback;
    }

    private static string? GetString(JsonElement? el, string key, string? fallback) {
        if (el is { ValueKind: JsonValueKind.Object } j
            && j.TryGetProperty(key, out var prop)
            && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return fallback;
    }

    private static bool GetBool(JsonElement? el, string key, bool fallback) {
        if (el is { ValueKind: JsonValueKind.Object } j
            && j.TryGetProperty(key, out var prop))
            return prop.ValueKind switch {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => fallback
            };
        return fallback;
    }
}
