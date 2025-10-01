using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public sealed class GeminiLlmChat : ILlmChat {
    private readonly IhttpsClientHelper _http;
    private readonly LoggerAIOptions _opt;
    IConfiguration _configuration;
    public GeminiLlmChat(IhttpsClientHelperFactory factory, IOptions<LoggerAIOptions> opt, IConfiguration configuration) {
        _http = factory.CreateOrGet(opt.Value.httpClientName);
        _opt = opt.Value;
        _configuration = configuration;
    }
    public async Task<string> ChatAsync(IEnumerable<ChatPromptMessage> messages) {
        throw new NotImplementedException();
    }
    public async Task<string> ChatAsync(string system, string user) {
        var payload = new {
            systemInstruction = system,
            contents = new[]
            {
                    new
                    {
                        role = "user", // Il prompt dell'utente
                        parts = new[]
                        {
                            new { text = user}
                        }
                    }
                },
            config = new {
                temperature = _opt.Temperature
            }
        };


        return await ReadResponseContentAsync(payload);
    }
    private async Task<string> ReadResponseContentAsync(object payload) {
        IContentBuilder jsonBuilder = new JsonContentBuilder();


        var chatghapikey = _opt.chatghapikey;
        _http.setHeadersWithoutAuthorization(new Dictionary<string, string> { { "x-goog-api-key", chatghapikey } });

        _http.AddRequestAction(async (request, response, nr, ts) => {
            Console.WriteLine($"Request : {request}");
            await Task.CompletedTask;
        });

        string jsonPayload = JsonSerializer.Serialize(payload);

        using var resp = await _http.SendAsync(_opt.urlLLM, HttpMethod.Post, jsonPayload, jsonBuilder);

        resp.EnsureSuccessStatusCode();

        var data = await resp.Content.ReadFromJsonAsync<ChatResponse>(new CancellationToken());
        return data?.Candidates
                           .FirstOrDefault()
                           ?.Content
                           ?.Parts
                           .FirstOrDefault()
                           ?.Text ?? "Nessuna risposta valida trovata.";
    }


    // La classe principale che rappresenta l'intera risposta JSON
    private class ChatResponse {
        // L'array di risposte possibili. Di solito ne userai solo il primo (index 0).
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; } = new List<Candidate>();

        // Metadati relativi ai token consumati
        [JsonPropertyName("usageMetadata")]
        public UsageMetadata? UsageMetadata { get; set; }
    }

    // ----------------------------------------------------
    // Dettaglio: Il contenuto di ciascun candidato
    // ----------------------------------------------------
    private class Candidate {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }

        // Puoi ignorare "safetyRatings" per la definizione di base
        // [JsonPropertyName("safetyRatings")]
        // public List<SafetyRating>? SafetyRatings { get; set; }
    }

    // ----------------------------------------------------
    // Dettaglio: L'oggetto Content (il messaggio effettivo)
    // ----------------------------------------------------
    private class Content {
        [JsonPropertyName("role")]
        public string? Role { get; set; } // Dovrebbe essere "model"

        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new List<Part>();
    }

    // ----------------------------------------------------
    // Dettaglio: La parte del messaggio (dove si trova il testo)
    // ----------------------------------------------------
    private class Part {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        // Per risposte multimodali, ci sarebbero anche campi come "inlineData" o "fileData"
    }

    // ----------------------------------------------------
    // Dettaglio: Metadati sull'utilizzo
    // ----------------------------------------------------
    private class UsageMetadata {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }
    }
}

