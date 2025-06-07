using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;

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
    protected override Assembly? Load(AssemblyName name) {
        // Se mi chiedono 'CSharpEssentials.LoggerHelper' restituisco quella già caricata nel Default context
        if (string.Equals(name.Name, "CSharpEssentials.LoggerHelper", StringComparison.OrdinalIgnoreCase))
            return AssemblyLoadContext.Default.LoadFromAssemblyName(name);

        // altrimenti risolvo come facevi tu
        var path = _resolver.ResolveAssemblyToPath(name);
        return path != null
            ? LoadFromAssemblyPath(path)
            : null;
    }
}