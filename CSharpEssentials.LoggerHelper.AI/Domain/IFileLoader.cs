using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace CSharpEssentials.LoggerHelper.AI.Domain;
public interface IFileLoader {
    List<SQLLMModels> getModelSQLLMModels();
}
public sealed class FileLoader : IFileLoader {
    LoggerAIOptions _options;
    public FileLoader(IOptions<LoggerAIOptions> options) {
        _options = options.Value;
    }
    public List<SQLLMModels> getModelSQLLMModels() {
        if (!Directory.Exists(_options.FolderSqlLoaderContainer))
            return new List<SQLLMModels>();

        var path = _options.FolderSqlLoaderContainer;
        var directories = Directory.GetDirectories(path);
        var models = directories.Select(dirPath => 
            new SQLLMModels {
                action = Path.GetFileName(dirPath),
                contents = Directory.GetFiles(dirPath, "*.sql", SearchOption.AllDirectories).Select(
                    fl => new SQLLMModelContent {
                        fileName =  Path.GetFileName(fl),
                        content = File.ReadAllText(fl),
                        MarkdownFieldSelector = File.Exists(fl.Replace("sql", "txt")) ? File.ReadAllText(fl.Replace("sql", "txt")) : ""
                    }).ToList()
        }).ToList();
        return models;
    }
}
public static class FileLoaderExtensions {
    public static string getQuery(this List<SQLLMModels> models, string Name, string fileName) => 
        models.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == fileName)?.content;
    public static string getFieldTemplate(this List<SQLLMModels> models, string Name, string fileName) => 
        models.FirstOrDefault(a => a.action == Name).contents.FirstOrDefault(a => a.fileName == fileName)?.MarkdownFieldSelector;
    
}