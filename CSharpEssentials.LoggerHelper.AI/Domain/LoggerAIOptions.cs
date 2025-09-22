using System.ComponentModel.DataAnnotations;

namespace CSharpEssentials.LoggerHelper.AI.Domain;
public class LoggerAIOptions {
    public required string chatghapikey { get; set; }
    public required string FileSqlLoaderContainer { get; set; }
    [Range(0.0, 1.0)]
    public required double Temperature { get; set; }
    [Range(1, int.MaxValue)]
    public required int topScore { get; set; }
    public required string urlLLM { get; set; }
    public Dictionary<string, string>? headersLLM { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public required string httpClientName { get; set; }
}