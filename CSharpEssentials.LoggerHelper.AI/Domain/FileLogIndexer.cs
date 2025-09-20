using CSharpEssentials.LoggerHelper.AI.Infrastructure;

namespace CSharpEssentials.LoggerHelper.AI.Domain;
public class FileLogIndexer {
    private readonly IEmbeddingService _embeddingService;

    public FileLogIndexer(IEmbeddingService embeddingService) {
        _embeddingService = embeddingService;
    }

    public async Task<ILogVectorStore> IndexFileAsync(string filePath) {
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException($"Il file non è stato trovato: {filePath}");
        }

        var rawDocs = await File.ReadAllLinesAsync(filePath);
        var docsWithEmbeddings = new List<LogEmbedding>();

        foreach (var text in rawDocs) {
            // La logica per generare gli embedding si trova qui
            var embedding = await _embeddingService.EmbedAsync(text);

            docsWithEmbeddings.Add(new LogEmbedding(Guid.NewGuid().ToString(), "test", DateTimeOffset.UtcNow, embedding, text, null));
        }

        // Il repository in memoria viene istanziato e popolato in questo momento
        return new InMemoryLogVectorStore(docsWithEmbeddings);
    }
}