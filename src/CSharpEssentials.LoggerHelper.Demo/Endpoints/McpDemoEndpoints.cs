using CSharpEssentials.LoggerHelper.MCP;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Endpoint module 8 — MCP Server demo.
///
/// Shows how the LoggerHelper MCP server is wired (POST /mcp) and provides
/// a discovery endpoint listing available tools with example curl commands.
///
/// The actual MCP server lives at POST /mcp — this group is for documentation only.
/// </summary>
public class McpDemoEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/mcp-demo").WithTags("MCP Demo");

        group.MapGet("/tools", () => Results.Ok(new {
            description = "LoggerHelper MCP server is running at POST /mcp (JSON-RPC 2.0)",
            protocolVersion = "2024-11-05",
            availableTools = new[] {
                new {
                    name = "loggerhelper_get_health",
                    description = "Overall status: OK / WARNING / CRITICAL"
                },
                new {
                    name = "loggerhelper_get_errors",
                    description = "Recent sink errors (accepts optional count parameter)"
                },
                new {
                    name = "loggerhelper_get_sinks",
                    description = "All configured sinks with ACTIVE/FAILED status"
                },
                new {
                    name = "loggerhelper_get_config",
                    description = "App name, routing rules, masking and contextual logging settings"
                },
                new {
                    name = "loggerhelper_set_log_level",
                    description = "Change log level routing for a sink at runtime (no restart)"
                },
                new {
                    name = "loggerhelper_search_logs",
                    description = "Search recent logs in the contextual ring buffer by text/level"
                },
                new {
                    name = "loggerhelper_toggle_sink",
                    description = "Enable or disable a sink at runtime without restart"
                }
            },
            curlExamples = new {
                listTools = "curl -X POST http://localhost:5000/mcp -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}'",
                getHealth = "curl -X POST http://localhost:5000/mcp -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"loggerhelper_get_health\",\"arguments\":{}}}'",
                getErrors = "curl -X POST http://localhost:5000/mcp -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"loggerhelper_get_errors\",\"arguments\":{\"count\":5}}}'"
            }
        }))
        .WithSummary("MCP Server — available tools and curl examples")
        .WithDescription(
            "Returns the list of LoggerHelper MCP tools with curl examples. " +
            "The MCP server itself is at POST /mcp and speaks JSON-RPC 2.0. " +
            "Use it with Claude, Cursor, GitHub Copilot, or any MCP-compatible client to " +
            "inspect sink health, errors, and configuration with natural language.");

        group.MapPost("/call/{toolName}", (string toolName, LoggerHelperMcpTools tools) => {
            var text = toolName switch {
                "get-health"    => tools.GetHealth(),
                "get-errors"    => tools.GetErrors(),
                "get-sinks"     => tools.GetSinks(),
                "get-config"    => tools.GetConfig(),
                "search-logs"   => tools.SearchLogs(),
                "toggle-sink"   => "Use POST /mcp with JSON-RPC for toggle-sink (requires sink + enabled params)",
                "set-log-level" => "Use POST /mcp with JSON-RPC for set-log-level (requires sink + levels params)",
                _               => $"Unknown tool '{toolName}'. Valid: get-health, get-errors, get-sinks, get-config, search-logs, set-log-level, toggle-sink"
            };
            return Results.Ok(new { tool = toolName, result = text });
        })
        .WithSummary("Call a LoggerHelper MCP tool directly (REST shortcut)")
        .WithDescription(
            "REST shortcut for calling LoggerHelper MCP tools without the full JSON-RPC 2.0 envelope. " +
            "Valid toolName values: get-health, get-errors, get-sinks, get-config, search-logs, set-log-level, toggle-sink. " +
            "For production AI integration use POST /mcp instead.");
    }
}
