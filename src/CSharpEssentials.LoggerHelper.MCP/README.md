# CSharpEssentials.LoggerHelper.MCP

**Give your AI assistant eyes — and hands — on your logs.**

`CSharpEssentials.LoggerHelper.MCP` adds a zero-dependency [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server to any ASP.NET Core application that uses LoggerHelper. Point Claude Desktop, Claude Code, Cursor, or any MCP-compatible client at `/mcp` and ask in plain English:

- *"Are all sinks healthy?"*
- *"Search logs for payment failures in the last 5 minutes"*
- *"Set Console to Error and Fatal only — no restart needed"*
- *"Disable the Email sink during tonight's maintenance window"*

No dashboard to stand up. No extra infrastructure. One POST endpoint, **7 tools**, zero external dependencies.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper.MCP
```

Requires: `CSharpEssentials.LoggerHelper` ≥ 5.2.0

---

## Wire up (Program.cs)

```csharp
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.MCP;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoggerHelper(builder.Configuration);
builder.Services.AddLoggerHelperMcp();   // register MCP tools in DI

var app = builder.Build();
app.UseLoggerHelper();
app.MapLoggerHelperMcp("/mcp");          // expose POST /mcp (JSON-RPC 2.0)

app.Run();
```

---

## Available MCP Tools — 7 total

### Read-only (4 tools)

| Tool | Description |
|---|---|
| `loggerhelper_get_health` | Overall status: **OK** / **WARNING** / **CRITICAL**, active sink count, error count |
| `loggerhelper_get_errors` | Recent sink errors with timestamp, message, and stack trace. Accepts optional `count` parameter |
| `loggerhelper_get_sinks` | All configured sinks with **ACTIVE** / **FAILED** status and assigned log levels |
| `loggerhelper_get_config` | Application name, routing rules, sensitive data masking settings, contextual logging status |

### Action tools — new in v5.2.0 (3 tools)

| Tool | Parameters | Description |
|---|---|---|
| `loggerhelper_set_log_level` | `sink`, `levels` | Change log level routing for any sink at runtime — no restart needed |
| `loggerhelper_search_logs` | `query`, `level`, `count` | Query the contextual ring buffer with text and/or level filters |
| `loggerhelper_toggle_sink` | `sink`, `enabled` | Enable or disable any sink without application restart |

> `loggerhelper_search_logs` requires contextual logging enabled in your config:
> ```json
> "General": { "EnableContextualLogging": true, "ContextualBufferCapacity": 200 }
> ```

---

## Connect to Claude Desktop

Add to `claude_desktop_config.json`:
- **Mac:** `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "myapp-logger": {
      "url": "http://localhost:5000/mcp",
      "transport": "streamable-http"
    }
  }
}
```

Restart Claude Desktop. A 🔌 icon will appear showing the connected tools.

---

## Connect to Claude Code (CLI)

```bash
# IMPORTANT: always specify --transport http
claude mcp add --transport http myapp-logger http://localhost:5000/mcp
```

> ⚠️ Without `--transport http`, Claude Code defaults to `stdio` (local process) and the connection will fail with `× failed`. The `--transport` flag is mandatory for HTTP servers.

Verify the connection:
```bash
claude mcp list
# Expected output:
# > myapp-logger · ✓ connected   (7 tools)
```

---

## How to query tools — natural language, not commands

`/mcp` in Claude Code is a **discovery command** — it shows tool metadata but does not call anything.

To actually get data from your app, **ask in plain English** in the chat:

| What you type | Tool called internally |
|---|---|
| `"What is the health of my app?"` | `loggerhelper_get_health` |
| `"Are there any sink errors?"` | `loggerhelper_get_errors` |
| `"What sinks are configured and their status?"` | `loggerhelper_get_sinks` |
| `"Show me the current logging configuration"` | `loggerhelper_get_config` |
| `"Search logs for NullReferenceException"` | `loggerhelper_search_logs` |
| `"Set Console sink to Error and Fatal only"` | `loggerhelper_set_log_level` |
| `"Disable the Email sink for maintenance"` | `loggerhelper_toggle_sink` |

**What happens under the hood:**
```
You type:  "Are there any errors?"
     ↓
Claude understands intent → picks loggerhelper_get_errors
     ↓
POST /mcp  {"method":"tools/call","params":{"name":"loggerhelper_get_errors","arguments":{"count":10}}}
     ↓
Your app returns:  { "errors": [...] }
     ↓
Claude responds in natural language with real data from your app
```

---

## Connect to Cursor

In Cursor settings → MCP section, add:

```json
{
  "mcpServers": {
    "myapp-logger": {
      "url": "http://localhost:5000/mcp",
      "transport": "http"
    }
  }
}
```

---

## Test without an AI client (curl)

```bash
# List all 7 available tools
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

# Search logs for a keyword
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"loggerhelper_search_logs","arguments":{"query":"payment","count":20}}}'

# Change Console to Error+Fatal only (runtime, no restart)
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"loggerhelper_set_log_level","arguments":{"sink":"Console","levels":"Error,Fatal"}}}'

# Toggle a sink off
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"loggerhelper_toggle_sink","arguments":{"sink":"Email","enabled":false}}}'
```

**PowerShell equivalent:**
```powershell
curl http://localhost:5000/mcp `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
```

---

## Troubleshooting

### `× failed` after `claude mcp add`

The most common causes:

| Symptom | Cause | Fix |
|---|---|---|
| `× failed` immediately | `--transport http` not specified | Remove and re-add: `claude mcp add --transport http myapp-logger <url>` |
| `× failed` with app running | App not listening on that port | Check `launchSettings.json` for the correct port |
| `× failed` after correct command | `/mcp` endpoint not mapped | Add `app.MapLoggerHelperMcp("/mcp")` to `Program.cs` |
| `× failed` → tools return empty | `AddLoggerHelperMcp()` missing | Add `builder.Services.AddLoggerHelperMcp()` to `Program.cs` |

### Remove a misconfigured server

```bash
claude mcp remove myapp-logger
```

### Verify the endpoint is reachable before connecting Claude

```bash
curl http://localhost:5000/mcp \
  -X POST -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
```
If you get a JSON response with `"tools":[...]` the server is healthy. If `Connection refused`, the app is not running on that port.

---

## AI client compatibility

| Client | Transport | Status |
|---|---|---|
| Claude Desktop | `streamable-http` | ✅ Full support |
| Claude Code (CLI) | `http` (via `--transport http`) | ✅ Full support |
| Cursor | `http` | ✅ Full support |
| GitHub Copilot | MCP (expanding) | ⚠️ Partial — depends on version |
| Gemini | — | ❌ Uses a different tool-use protocol, not MCP-compatible |
| Any HTTP client | JSON-RPC 2.0 | ✅ Call `/mcp` directly with curl or HttpClient |

---

## Transport details

Implements the **Streamable HTTP** transport (MCP spec `2024-11-05`):

- `POST /mcp` → JSON-RPC 2.0 request/response
- Supported methods: `initialize`, `tools/list`, `tools/call`
- Zero external dependencies — pure `System.Text.Json` + `Microsoft.AspNetCore.App`

---

## Why this matters

Serilog and NLog have no built-in AI tooling. With LoggerHelper MCP:

- AI assistants can **diagnose** logging issues without reading log files
- AI assistants can **act** — change levels, toggle sinks — not just observe
- Zero additional infrastructure (no Seq, no Kibana, no Grafana)
- Works in production with `RequireAuthorization` protecting the endpoint
- Runtime changes survive without restart; original config restored on restart

---

## Docs

- [loggerhelper.it](https://www.loggerhelper.it)
- [GitHub](https://github.com/alexbypa/CSharp.Essentials)
- [Full Changelog](https://github.com/alexbypa/CSharp.Essentials/blob/main/CHANGELOG.md)