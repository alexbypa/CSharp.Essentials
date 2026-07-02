# CSharpEssentials.LoggerHelper.MCP

**Give your AI assistant eyes into your logs.**

`CSharpEssentials.LoggerHelper.MCP` adds a zero-dependency [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server to any ASP.NET Core application that uses LoggerHelper. Point Claude, Cursor, GitHub Copilot, or any MCP-compatible client at `/mcp` and ask questions like:

- *"Are all sinks healthy?"*
- *"Show me the last 10 logging errors"*
- *"What levels does the Email sink receive?"*

No dashboard to stand up. No extra infrastructure. Just one POST endpoint and four tools.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper.MCP
```

Requires: `CSharpEssentials.LoggerHelper` ≥ 5.1.0

---

## Wire up (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoggerHelper(builder.Configuration);
builder.Services.AddLoggerHelperMcp();   // ← register MCP tools

var app = builder.Build();
app.UseLoggerHelper();
app.MapLoggerHelperMcp("/mcp");          // ← expose POST /mcp

app.Run();
```

---

## Available MCP Tools

| Tool | Description |
|---|---|
| `loggerhelper_get_health` | Overall status: OK / WARNING / CRITICAL, sink count, error count |
| `loggerhelper_get_errors` | Recent sink errors (accepts optional `count` parameter) |
| `loggerhelper_get_sinks` | All configured sinks with ACTIVE/FAILED status and log levels |
| `loggerhelper_get_config` | App name, routing rules, and masking settings |

---

## Example: Call via curl

```bash
# List available tools
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'

# Get health status
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"loggerhelper_get_health","arguments":{}}}'

# Get last 5 errors
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"loggerhelper_get_errors","arguments":{"count":5}}}'
```

---

## Connect to Claude Desktop / Cursor

Add to your MCP client config:

```json
{
  "mcpServers": {
    "loggerhelper": {
      "url": "http://localhost:5000/mcp",
      "transport": "http"
    }
  }
}
```

Then ask Claude: *"Check my LoggerHelper sink health"* — and it will call `loggerhelper_get_health` and return a natural-language summary.

---

## Why this matters

Serilog and NLog have no built-in AI tooling. With LoggerHelper MCP:
- AI assistants can diagnose logging issues without reading log files
- Zero additional infrastructure (no Seq, no Kibana, no dashboard)
- Works with any MCP client (Claude, Cursor, VS Code Copilot, custom agents)
- Zero external dependencies — pure `System.Text.Json` + ASP.NET Core

---

## Transport

The MCP server implements the **Streamable HTTP** transport (MCP spec `2024-11-05`):
- `POST /mcp` → JSON-RPC 2.0 request/response

Supported JSON-RPC methods: `initialize`, `tools/list`, `tools/call`.

---

## Docs

- [loggerhelper.com](https://www.loggerhelper.com)
- [GitHub](https://github.com/alexbypa/CSharp.Essentials)
