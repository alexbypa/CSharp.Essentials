using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public sealed class GeminiLlmChat : ILlmChat {
    private readonly IhttpsClientHelper _http;
    private readonly LoggerAIOptions _opt;
    IConfiguration _configuration;
    private readonly LLMModels _lLMModels;
    public GeminiLlmChat(IhttpsClientHelperFactory factory, IOptions<LoggerAIOptions> opt, IConfiguration configuration, List<LLMModels> lLMModels) {
        _http = factory.CreateOrGet(opt.Value.httpClientName);
        _opt = opt.Value;
        _configuration = configuration;
        _lLMModels = lLMModels.getmodel(_opt.Name);
    }
    public async Task<string> ChatAsync(IEnumerable<ChatPromptMessage> messages) {
        string finalPayload = _opt.RequestTemplate;
        string systemText = messages.FirstOrDefault(m => m.Role == "system")?.Content ?? "You are a helpful assistant.";
        string userText = messages.FirstOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        string assistantText = messages.FirstOrDefault(m => m.Role == "model" || m.Role == "assistant")?.Content ?? string.Empty;

        var jsonPayload = _lLMModels.RequestTemplate
            .Replace("@system", systemText)
            .Replace("@user", userText)
            .Replace("@assistant", assistantText)
            .Replace("@temperature", _opt.Temperature.ToString().Replace(",", "."));

        return await ReadResponseContentAsync(jsonPayload);
    }
    public async Task<string> ChatAsync(string system, string user) {
        throw new NotImplementedException();
    }
    private async Task<string> ReadResponseContentAsync(string jsonPayload) {
        IContentBuilder jsonBuilder = new JsonContentBuilder();
        var chatghapikey = _opt.chatghapikey;
        /////////////_http.setHeadersWithoutAuthorization(new Dictionary<string, string> { { "x-goog-api-key", chatghapikey } });

        _http.setHeadersWithoutAuthorization(_opt.headersLLM);

        _http.AddRequestAction(async (request, response, nr, ts) => {
            Console.WriteLine($"Request : {request}");
            await Task.CompletedTask;
        });

        //string jsonPayload = JsonSerializer.Serialize(payload);

        using var resp = await _http.SendAsync(_opt.urlLLM, HttpMethod.Post, jsonPayload, jsonBuilder);

        resp.EnsureSuccessStatusCode();
        string jsonResponse = await resp.Content.ReadAsStringAsync();

        string[] pathSteps = _lLMModels.ResponseTemplate.Trim().Split(",");
        JsonNode? rootNode = JsonNode.Parse(jsonResponse);
        JsonNode? textNode = rootNode.GetNodeByPath(pathSteps);

        return textNode.ToString();
    }
}


