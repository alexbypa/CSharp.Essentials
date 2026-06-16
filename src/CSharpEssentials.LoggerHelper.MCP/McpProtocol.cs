using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpEssentials.LoggerHelper.MCP;

/// <summary>
/// Represents a JSON-RPC 2.0 request as defined by the Model Context Protocol spec.
/// </summary>
public sealed class McpRequest {
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = "";

    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// Represents a JSON-RPC 2.0 response returned by the MCP server.
/// </summary>
public sealed class McpResponse {
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc => "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; init; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpError? Error { get; init; }
}

/// <summary>
/// Represents a JSON-RPC 2.0 error object.
/// </summary>
public sealed class McpError {
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = "";
}

/// <summary>
/// Describes an MCP tool exposed by the server (returned by tools/list).
/// </summary>
public sealed class McpToolDefinition {
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; init; } = new { type = "object", properties = new { } };
}
