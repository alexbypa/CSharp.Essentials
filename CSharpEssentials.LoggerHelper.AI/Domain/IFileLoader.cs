using Microsoft.Extensions.Options;

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
        var path = _options.FolderSqlLoaderContainer;
        var directories = Directory.GetDirectories(path);
        var models = directories.Select(dirPath => 
            new SQLLMModels {
                action = Path.GetFileName(dirPath),
                contents = Directory.GetFiles(dirPath, "*.sql", SearchOption.AllDirectories).Select(
                    fl => new SQLLMModelContent {
                        fileName = Path.GetFileName(fl),
                        content = File.ReadAllText(fl)
                    }).ToList()
        }).ToList();
        return models;
    }
}