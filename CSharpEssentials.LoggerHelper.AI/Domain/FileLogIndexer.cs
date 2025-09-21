namespace CSharpEssentials.LoggerHelper.AI.Domain;
/// <summary>
/// REVIEW: FileLogIndexer.cs
/// Questo file rappresenta il "Costruttore di Dati" nel nostro sistema.
/// La sua UNICA responsabilità (in accordo con il Single Responsibility Principle)
/// è quella di prendere una sorgente dati (un file o uno stream) e trasformarla
/// in una lista di oggetti 'LogEmbedding', arricchiti con i vettori generati
/// dall'embedding service. Non sa e non deve sapere dove questi dati verranno salvati.
/// </summary>
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