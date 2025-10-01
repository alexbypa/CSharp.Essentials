using CSharpEssentials.LoggerHelper.model;
using System.ComponentModel.DataAnnotations;

namespace CSharpEssentials.LoggerHelper.AI.Domain;

[CustomValidation(typeof(LoggerAIOptionsValidator), nameof(LoggerAIOptionsValidator.ValidateOptions))]
public class LoggerAIOptions {
    public string chatghapikey { get; set; }
    [CustomValidation(typeof(LoggerAIOptionsValidator), nameof(LoggerAIOptionsValidator.checkPathSqlQuery))]
    public string FolderSqlLoaderContainer { get; set; }
    [CustomValidation(typeof(LoggerAIOptionsValidator), nameof(LoggerAIOptionsValidator.ValidateTemperature))]
    public double? Temperature { get; set; }
    [Range(1, int.MaxValue)]
    public int? topScore { get; set; }
    public string urlLLM { get; set; }
    public Dictionary<string, string>? headersLLM { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public required string httpClientName { get; set; }
    public string RequestTemplate { get; set; }
}

public static class LoggerAIOptionsValidator {
    private static void AddValidationError(string errorMessage, string memberName) {
        GlobalLogger.Errors.Add(new LogErrorEntry {
            ContextInfo = $"AI OptionsValidationException for {memberName}",
            ErrorMessage = errorMessage,
            SinkName = "LoggerHelper.AI",
            Timestamp = DateTime.UtcNow
        });
    }
    public static ValidationResult ValidateOptions(LoggerAIOptions options, ValidationContext context) {
        if (string.IsNullOrWhiteSpace(options.chatghapikey)) 
            AddValidationError("The 'chatghapikey' key was not found or is empty. It is a required field.", "chatghapikey");
        
        if (string.IsNullOrWhiteSpace(options.FolderSqlLoaderContainer))
            AddValidationError("The 'FolderSqlLoaderContainer' key was not found or is empty. It is a required field.", "FolderSqlLoaderContainer");

        if (!options.topScore.HasValue)
            AddValidationError("The 'topScore' key was not found or is empty. It is a required field.", "topScore");

        if (string.IsNullOrEmpty(options.urlLLM))
            AddValidationError("The 'urlLLM' key was not found or is empty. It is a required field.", "urlLLM");

        if (!options.Temperature.HasValue || options.Temperature.Value < 0.0 || options.Temperature.Value > 1.0) 
            AddValidationError("The 'Temperature' field must be a value between 0.0 and 1.0.", "Temperature");


        return ValidationResult.Success!;
    }
    public static ValidationResult ValidateTemperature(double temperature, ValidationContext context) {
        if (temperature < 0.0 || temperature > 1.0)
            AddValidationError("The field Temperature must be between 0 and 1. Please check key [LoggerAIOptions:Temperature] appsettings.json file.", "Temperature");
        
        return ValidationResult.Success!;    
    }
    public static ValidationResult checkPathSqlQuery(string FolderSqlLoaderContainer, ValidationContext context) {
        if (!Directory.Exists(FolderSqlLoaderContainer)) 
            AddValidationError("The folder settled on FolderSqlLoaderContainer not exists. Please check key [LoggerAIOptions:FolderSqlLoaderContainer] appsettings.json file.", "FolderSqlLoaderContainer");

        return ValidationResult.Success;    
    }

}

