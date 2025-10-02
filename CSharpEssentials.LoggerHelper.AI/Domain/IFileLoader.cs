using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.AI.Domain;
public interface IFileLoader {
    List<SQLLMModels> getModelSQLLMModels();
    List<LLMModels> getModelLLMModels();
}
public sealed class FileLoader : IFileLoader {
    LoggerAIOptions _options;
    public FileLoader(IOptions<LoggerAIOptions> options) {
        _options = options.Value;
    }
    public List<LLMModels> getModelLLMModels() {
        if (!Directory.Exists(_options.FolderAIModelsLoaderContainer))
            return new List<LLMModels>();
        var path = _options.FolderAIModelsLoaderContainer;
        var directories = Directory.GetDirectories(path);
        var AIModels = directories.Select(dirPath =>
            new LLMModels {
                modelName = Path.GetFileNameWithoutExtension(dirPath),
                RequestTemplate = Directory.GetFiles(dirPath, "request.*", SearchOption.AllDirectories).FirstOrDefault() != null 
                    ? File.ReadAllText(Directory.GetFiles(dirPath, "request.*", SearchOption.AllDirectories).FirstOrDefault()) 
                    : "",
                 ResponseTemplate = Directory.GetFiles(dirPath, "response.*", SearchOption.AllDirectories).FirstOrDefault() != null 
                    ? File.ReadAllText(Directory.GetFiles(dirPath, "response.*", SearchOption.AllDirectories).FirstOrDefault()) 
                    : ""
            }).ToList();
        return AIModels;
    }
    public List<SQLLMModels> getModelSQLLMModels() {
        if (!Directory.Exists(_options.FolderSqlLoaderContainer))
            return new List<SQLLMModels>();

        var path = _options.FolderSqlLoaderContainer;
        var directories = Directory.GetDirectories(path);
        var SQLModels = directories.Select(dirPath => 
            new SQLLMModels {
                action = Path.GetFileName(dirPath),
                contents = Directory.GetFiles(dirPath, "*.sql", SearchOption.AllDirectories).Select(
                    fl => new SQLLMModelContent {
                        fileName =  Path.GetFileName(fl),
                        content = File.ReadAllText(fl),
                        MarkdownFieldSelector = File.Exists(fl.Replace("sql", "txt")) ? File.ReadAllText(fl.Replace("sql", "txt")) : "",
                        getMetricDetails = File.Exists(fl.Replace("sql", "json")) 
                        ? JsonSerializer.Deserialize<MetricDetails>(File.ReadAllText(fl.Replace("sql", "json")))
                        : null,
                    }).ToList()
        }).ToList();
        return SQLModels;
    }
}
public static class FileLoaderExtensions {
    public static string getQuery(this List<SQLLMModels> models, string Name, string fileName) => 
        models.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == fileName)?.content;
    public static string getFieldTemplate(this List<SQLLMModels> models, string Name, string fileName) => 
        models.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == fileName)?.MarkdownFieldSelector;
    public static MetricDetails getMetrics(this List<SQLLMModels> models, string Name, string fileName) => 
        models.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == fileName).getMetricDetails;
    public static LLMModels getmodel(this List<LLMModels> models, string Name) => 
        models.FirstOrDefault(a => a.modelName == Name);
}