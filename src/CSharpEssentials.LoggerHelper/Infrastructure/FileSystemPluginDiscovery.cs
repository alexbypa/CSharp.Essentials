using CSharpEssentials.LoggerHelper.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Discovers sink plugin assemblies from the application base directory.
/// Scans for CSharpEssentials.LoggerHelper.Sink.*.dll and registers their ISinkPlugin
/// implementations — either via [ModuleInitializer] (for freshly loaded assemblies)
/// or via reflection fallback (for assemblies already loaded by the runtime via project references).
/// </summary>
internal sealed class FileSystemPluginDiscovery : IPluginDiscovery {
    public void DiscoverAndLoad(ILogErrorStore errorStore) {
        var baseDir = AppContext.BaseDirectory;
        var pluginDlls = Directory.EnumerateFiles(baseDir, "CSharpEssentials.LoggerHelper.Sink.*.dll");

        foreach (var dll in pluginDlls) {
            try {
                var assemblyName = AssemblyName.GetAssemblyName(dll);

                // Check if the assembly is already loaded (e.g. via project reference).
                // In that case LoadFromAssemblyPath would throw or return the existing assembly
                // without re-running the [ModuleInitializer]. We use the existing instance.
                var existing = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(
                        a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

                var asm = existing ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);

                // [ModuleInitializer] has run if the assembly was freshly loaded above.
                // For already-loaded assemblies it may not have run (lazy load timing).
                // Reflection fallback ensures plugins are always registered.
                RegisterPluginsFromAssembly(asm, errorStore);
            } catch (Exception ex) {
                errorStore.Add(new LogErrorEntry {
                    SinkName = Path.GetFileNameWithoutExtension(dll),
                    ErrorMessage = $"Failed to load plugin: {ex.Message}",
                    ContextInfo = baseDir
                });
            }
        }
    }

    /// <summary>
    /// Scans an assembly for ISinkPlugin implementations and registers any that are
    /// not already present in the registry (prevents duplicates with [ModuleInitializer]).
    /// </summary>
    private static void RegisterPluginsFromAssembly(Assembly asm, ILogErrorStore errorStore) {
        Type[] types;
        try {
            types = asm.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            errorStore.Add(new LogErrorEntry {
                SinkName = asm.GetName().Name ?? "Unknown",
                ErrorMessage = $"Failed to scan assembly for plugins: {ex.LoaderExceptions.FirstOrDefault()?.Message}"
            });
            return;
        }

        foreach (var type in types) {
            if (!type.IsClass || type.IsAbstract || !typeof(ISinkPlugin).IsAssignableFrom(type))
                continue;

            // Skip if a plugin of this exact type is already registered
            if (SinkPluginRegistry.All.Any(p => p.GetType() == type))
                continue;

            try {
                if (Activator.CreateInstance(type, nonPublic: true) is ISinkPlugin plugin)
                    SinkPluginRegistry.Register(plugin);
            } catch (Exception ex) {
                errorStore.Add(new LogErrorEntry {
                    SinkName = type.Name,
                    ErrorMessage = $"Failed to instantiate plugin {type.FullName}: {ex.Message}"
                });
            }
        }
    }
}
