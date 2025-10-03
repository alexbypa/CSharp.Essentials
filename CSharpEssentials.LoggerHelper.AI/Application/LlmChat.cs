using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Ports;
using CSharpEssentials.LoggerHelper.AI.Shared;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public sealed class LlmChat: ILlmChat {
    private readonly IhttpsClientHelper _http;
    private readonly LoggerAIOptions _opt;
    private readonly LLMModels _lLMModels;
    private IPayloadLLM _payload;
    public LlmChat(IhttpsClientHelperFactory factory, IOptions<LoggerAIOptions> opt, List<LLMModels> lLMModels, IPayloadLLM payload) {
        _http = factory.CreateOrGet(opt.Value.httpClientName);
        _opt = opt.Value;
        _lLMModels = lLMModels.getmodel(_opt.Name);
        _payload = payload;
    }

    public async Task<string> ChatAsync(IEnumerable<ChatPromptMessage> messages) {
        string systemText = messages.FirstOrDefault(m => m.Role == "system")?.Content ?? "You are a helpful assistant.";
        string userText = messages.FirstOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        string assistantText = messages.FirstOrDefault(m => m.Role == "model" || m.Role == "assistant")?.Content ?? string.Empty;
        
        return await ReadResponseContentAsync(_payload.buildPayload(systemText, userText, assistantText));
    }
    private async Task<string> ReadResponseContentAsync(string jsonPayload) {
        IContentBuilder jsonBuilder = new JsonContentBuilder();

        if (_opt.headersLLM != null)
            _http.setHeadersWithoutAuthorization(_opt.headersLLM);

        using var resp = await _http.SendAsync(_opt.urlLLM, HttpMethod.Post, jsonPayload, jsonBuilder);

        resp.EnsureSuccessStatusCode();
        string jsonResponse = await resp.Content.ReadAsStringAsync();

        string[] pathSteps = _lLMModels.ResponseTemplate.Trim().Split(",");
        JsonNode? rootNode = JsonNode.Parse(jsonResponse);
        JsonNode? textNode = rootNode?.GetNodeByPath(pathSteps) ?? JsonNode.Parse("{}");

        return textNode?.ToString() ?? "";
    }
}


