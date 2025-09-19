// OpenAiLlmChat.cs
using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpEssentials.LoggerHelper.AI.Application {
    // Usa l'endpoint GitHub Models compatibile OpenAI: /chat/completions
    public sealed class OpenAiLlmChat : ILlmChat {
        private readonly IhttpsClientHelper _http;
        private readonly LlmOptions _opt;
        IConfiguration _configuration;
        public OpenAiLlmChat(IhttpsClientHelperFactory factory, IOptions<LlmOptions> opt, IConfiguration configuration) {
            _http = factory.CreateOrGet("testAI"); // TODO: da aggiornare -> nome della configurazione in appsettings.json
            _opt = opt.Value;
            _configuration = configuration;
        }

        public async Task<string> ChatAsync(IEnumerable<ChatPromptMessage> messages, double temperature = 0) {
            var payload = new {
                model = "gpt-4o-mini",
                temperature,
                messages = messages
                .Select(m => new { role = m.Role, content = m.Content })
                .ToArray()
            };
            return await ReadResponseContentAsync(payload);
        }
        public async Task<string> ChatAsync(string system, string user, double temperature) {
            var payload = new {
                model = _opt.Model,
                temperature = _opt.DefaultTemperature,
                messages = new[]{
                    new { role = "system", content = system ?? string.Empty },
                    new { role = "user",   content = user   ?? string.Empty }
                }
            };
            return await ReadResponseContentAsync(payload);
        }
        private async Task<string> ReadResponseContentAsync(object payload) {
            IContentBuilder jsonBuilder = new JsonContentBuilder();

            Dictionary<string, string> headers = new Dictionary<string, string> { { "accept", "application/json" }, { "X-GitHub-Api-Version", "2023-10-01" } };

            var chatghapikey = _configuration.GetValue<string>("chat-gh-apikey");
            _http.setHeadersAndBearerAuthentication(headers, new httpsClientHelper.httpClientAuthenticationBearer(chatghapikey));

            _http.AddRequestAction(async (request, response, nr, ts) => {
                Console.WriteLine($"Request : {request}");
                await Task.CompletedTask;
            });

            string jsonPayload = JsonSerializer.Serialize(payload);

            using var resp = await _http.SendAsync("https://models.inference.ai.azure.com/chat/completions", HttpMethod.Post, jsonPayload, jsonBuilder);

            resp.EnsureSuccessStatusCode();

            var data = await resp.Content.ReadFromJsonAsync<ChatResponse>(new CancellationToken());
            return data?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }

        // DTO minimi per risposta OpenAI-compatibile
        private sealed class ChatResponse {
            [JsonPropertyName("choices")]
            public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        }

        private sealed class Choice {
            [JsonPropertyName("message")]
            public ChatMessage Message { get; set; } = new ChatMessage();
        }

        private sealed class ChatMessage {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "";

            [JsonPropertyName("content")]
            public string Content { get; set; } = "";
        }
    }
}
