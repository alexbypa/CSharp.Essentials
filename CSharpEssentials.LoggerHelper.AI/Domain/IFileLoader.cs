using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CSharpEssentials.LoggerHelper.AI.Domain;
public interface IFileLoader {
    string getSqlQuery();
}

public sealed class FileLoader : IFileLoader {
    LoggerAIOptions _options;
    public FileLoader(IOptions<LoggerAIOptions> options) {
        _options = options.Value;
    }
    public string getSqlQuery() {
        string path = _options.FileSqlLoaderContainer;
        string sql = File.ReadAllText(path);
        return sql;
    }
}