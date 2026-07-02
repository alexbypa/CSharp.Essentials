using CSharpEssentials.LoggerHelper.MCP;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Endpoint module 8 — MCP Server demo.
///
/// Exposes discovery and REST shortcuts for the two LoggerHelper MCP transports:
///   • Streamable HTTP  — POST /mcp           (Claude Code, Cursor, Copilot)
///   • HTTP+SSE         — GET /mcp/sse + POST /mcp/messages  (Claude Desktop, MCP Inspector)
/// </summary>
public class McpDemoEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/mcp-demo").WithTags("MCP Demo");

        group.MapGet("/tools", () => Results.Ok(new {
            description     = "LoggerHelper MCP server — two transports available",
            protocolVersion = "2024-11-05",
            transports = new {
                streamableHttp = new {
                    description = "POST /mcp (JSON-RPC 2.0) — recommended for Claude Code, Cursor, Copilot",
                    endpoint    = "POST /mcp"
                },
                sse = new {
                    description = "GET /mcp/sse + POST /mcp/messages — required by Claude Desktop and MCP Inspector",
                    sseEndpoint      = "GET  /mcp/sse",
                    messagesEndpoint = "POST /mcp/messages?sessionId=<id>"
                }
            },
            availableTools = new[] {
                new { name = "loggerhelper_get_health",  description = "Overall status: OK / WARNING / CRITICAL" },
                new { name = "loggerhelper_get_errors",  description = "Recent sink errors (accepts optional count parameter)" },
                new { name = "loggerhelper_get_sinks",   description = "All configured sinks with ACTIVE/FAILED status" },
                new { name = "loggerhelper_get_config",  description = "App name, routing rules, and masking settings" }
            },
            availablePrompts = new[] {
                new {
                    name        = "diagnose-logging",
                    description = "Diagnose all LoggerHelper sinks and configuration",
                    argument    = "focus: health | errors | sinks | config | all (default: all)"
                }
            },
            curlExamples = new {
                listTools       = "curl -X POST http://localhost:5000/mcp -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}'",
                getHealth       = "curl -X POST http://localhost:5000/mcp -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"loggerhelper_get_health\",\"arguments\":{}}}'",
                getErrors       = "curl -X POST http://localhost:5000/mcp -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"loggerhelper_get_errors\",\"arguments\":{\"count\":5}}}'",
                listPrompts     = "curl -X POST http://localhost:5000/mcp -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"prompts/list\",\"params\":{}}'",
                diagnoseAll     = "curl -X POST http://localhost:5000/mcp -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"prompts/get\",\"params\":{\"name\":\"diagnose-logging\",\"arguments\":{\"focus\":\"all\"}}}'",
                sseConnect      = "curl -N http://localhost:5000/mcp/sse"
            }
        }))
        .WithSummary("MCP Server — transports, tools, prompts, and curl examples")
        .WithDescription(
            "Returns the LoggerHelper MCP transport details, available tools, available prompts, and curl examples. " +
            "Streamable HTTP (POST /mcp) for Claude Code / Cursor / Copilot; " +
            "HTTP+SSE (GET /mcp/sse + POST /mcp/messages) for Claude Desktop and MCP Inspector.");

        group.MapGet("/prompts", () => Results.Ok(LoggerHelperMcpPrompts.BuildList()))
        .WithSummary("MCP Prompts — list predefined prompts")
        .WithDescription(
            "Returns the list of predefined MCP prompts. " +
            "Use 'prompts/get' with name='diagnose-logging' on POST /mcp to retrieve the full prompt messages.");

        group.MapPost("/call/{toolName}", (string toolName, LoggerHelperMcpTools tools) => {
            var text = toolName switch {
                "get-health" => tools.GetHealth(),
                "get-errors" => tools.GetErrors(),
                "get-sinks"  => tools.GetSinks(),
                "get-config" => tools.GetConfig(),
                _            => $"Unknown tool '{toolName}'. Valid: get-health, get-errors, get-sinks, get-config"
            };
            return Results.Ok(new { tool = toolName, result = text });
        })
        .WithSummary("Call a LoggerHelper MCP tool directly (REST shortcut)")
        .WithDescription(
            "REST shortcut for calling LoggerHelper MCP tools without the JSON-RPC 2.0 envelope. " +
            "Valid toolName values: get-health, get-errors, get-sinks, get-config. " +
            "For AI integration use POST /mcp (Streamable HTTP) or GET /mcp/sse (SSE) instead.");
    }
}
