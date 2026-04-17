using CSharpEssentials.LoggerHelper.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Discovers sink plugin assemblies from the application base directory.
/// Scans for CSharpEssentials.LoggerHelper.Sink.*.dll and loads them
/// into the default AssemblyLoadContext, triggering [ModuleInitializer] auto-registration.
/// </summary>
internal sealed class FileSystemPluginDiscovery : IPluginDiscovery {
    public void DiscoverAndLoad(ILogErrorStore errorStore) {
        var baseDir = AppContext.BaseDirectory;
        var pluginDlls = Directory.EnumerateFiles(baseDir, "CSharpEssentials.LoggerHelper.Sink.*.dll");

        foreach (var dll in pluginDlls) {
            try {
                _ = AssemblyName.GetAssemblyName(dll);
                AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
            } catch (Exception ex) {
                errorStore.Add(new LogErrorEntry {
                    SinkName = Path.GetFileNameWithoutExtension(dll),
                    ErrorMessage = $"Failed to load plugin: {ex.Message}",
                    ContextInfo = baseDir
                });
            }
        }
    }
}
