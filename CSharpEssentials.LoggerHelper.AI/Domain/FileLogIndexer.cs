namespace CSharpEssentials.LoggerHelper.AI.Domain;
public class FileLogIndexer {
    private readonly IEmbeddingService _embeddingService;

    public FileLogIndexer(IEmbeddingService embeddingService) {
        _embeddingService = embeddingService;
    }
    public async Task<List<LogEmbedding>> IndexStreamAsync(Stream stream) {
        var docsWithEmbeddings = new List<LogEmbedding>();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null) {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var embedding = await _embeddingService.EmbedAsync(line);
            docsWithEmbeddings.Add(new LogEmbedding(Guid.NewGuid().ToString(), "dynamic-upload", DateTimeOffset.UtcNow, embedding, line, null));
        }

        return docsWithEmbeddings;
    }
}