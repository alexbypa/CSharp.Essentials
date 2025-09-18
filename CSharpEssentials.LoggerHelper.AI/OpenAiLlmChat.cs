// OpenAiLlmChat.cs
using CSharpEssentials.LoggerHelper.AI.Domain;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CSharpEssentials.LoggerHelper.AI {
    // Usa l'endpoint GitHub Models compatibile OpenAI: /chat/completions
    public sealed class OpenAiLlmChat : ILlmChat {
        private readonly HttpClient _http;
        private readonly LlmOptions _opt;

        public OpenAiLlmChat(IHttpClientFactory factory, IOptions<LlmOptions> opt) {
            _http = factory.CreateClient("ghmodels");
            _opt = opt.Value;
        }

        public async Task<string> ChatAsync(string system, string user, double temperature) {
            var payload = new {
                model = _opt.Model,
                temperature = _opt.DefaultTemperature,
                messages = new[]
                {
                    new { role = "system", content = system ?? string.Empty },
                    new { role = "user",   content = user   ?? string.Empty }
                }
            };

            using var resp = await _http.PostAsJsonAsync("/chat/completions", payload);
            resp.EnsureSuccessStatusCode();

            var data = await resp.Content.ReadFromJsonAsync<ChatResponse>(new CancellationToken());
            return data?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }

        // DTO minimi per risposta OpenAI-compatibile
        private sealed class ChatResponse {
            [JsonPropertyName("choices")]
            public Choice[] Choices { get; set; } = System.Array.Empty<Choice>();
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
