namespace CSharpEssentials.LoggerHelper.AI.Domain;
public class LLMModels {
    public required string modelName { get; set; } // Ex. "gpt-4o", "gemini-pro", "gemini-1.5-turbo"
    public required string RequestTemplate { get; set; } // Ex. "You are a helpful assistant...."
    public required string ResponseTemplate { get; set; } // Ex. "You are a helpful assistant...."
}