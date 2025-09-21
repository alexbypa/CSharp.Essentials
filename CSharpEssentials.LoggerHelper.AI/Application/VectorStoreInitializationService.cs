using CSharpEssentials.LoggerHelper.AI.Domain;
using CSharpEssentials.LoggerHelper.AI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public class VectorStoreInitializationService : IHostedService {
    protected IServiceProvider _serviceProvider;

    public VectorStoreInitializationService(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider;
    }
    public async Task StartAsync(CancellationToken cancellationToken) {
        // È LUI che orchestra, non il costruttore!
        using var scope = _serviceProvider.CreateScope();

        var indexer = scope.ServiceProvider.GetRequiredService<FileLogIndexer>();
        var store = scope.ServiceProvider.GetRequiredService<ILogVectorStore>();

        // Il percorso del file dovrebbe venire dalla configurazione (es. appsettings.json)
        // e non essere hardcoded.
        
        var initialDocs = await indexer.IndexStreamAsync(File.OpenRead("C:\\Github\\rag.txt"));

        // Qui usiamo il metodo 'Populate'
        if (store is InMemoryLogVectorStore memoryStore) {
            memoryStore.Populate(initialDocs);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}