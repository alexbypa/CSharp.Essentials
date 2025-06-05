using System.Reflection;
using System.Runtime.Loader;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// AssemblyLoadContext “tollerante” che, quando tenta di risolvere un AssemblyName
/// (es. “Serilog.Formatting.Elasticsearch, Version=10.0.0.0 …”) e non lo trova,
/// cattura l’eccezione e ritorna null, evitando di far fallire l’intero caricamento.
/// </summary>
internal class TolerantPluginLoadContext : AssemblyLoadContext {
    private readonly AssemblyDependencyResolver _resolver;
    /// <summary>
    /// Il costruttore riceve il path completo al plugin (.dll). 
    /// L’AssemblyDependencyResolver userà questo path per capire la “cartella base” di quel plugin.
    /// </summary>
    public TolerantPluginLoadContext(string pluginPath) : base(isCollectible: false) {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }
    protected override Assembly Load(AssemblyName assemblyName) {
        try {
            // Provo a risolvere il path fisico di questo assembly (es. Serilog.Formatting.Elasticsearch.dll)
            string? path = _resolver.ResolveAssemblyToPath(assemblyName);
            if (path != null) {
                // Se trovo un file compatibile, provo a caricarlo da quel percorso
                return LoadFromAssemblyPath(path);
            }
        } catch {
            // In caso di qualunque errore (versione sbagliata, PublicKeyToken diverso, file mancante),
            // ritorno null e permetto al runtime di continuare con altri contesti o di ignorare.
        }

        // Se non l’ho risolto io, restituisco null in modo da lasciare al Default o ad altri contesti
        // la possibilità di caricarlo, oppure di ignorarlo del tutto.
        return null!;
    }
}