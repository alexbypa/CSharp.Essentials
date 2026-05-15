using CSharpEssentials.LoggerHelper.Diagnostics;
using System.Reflection;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Invokes source-generated sink registrations when present in the consuming assembly.
/// </summary>
internal sealed class CompileTimePluginDiscovery : IPluginDiscovery {
    private static readonly string RegistrationsTypeName =
        "CSharpEssentials.LoggerHelper.Generated.LoggerHelperSinkRegistrations";

    public void DiscoverAndLoad(ILogErrorStore errorStore) {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            var type = assembly.GetType(RegistrationsTypeName, throwOnError: false);
            if (type is null)
                continue;

            var method = type.GetMethod(
                "RegisterAll",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: [typeof(ISinkPluginRegistry)],
                modifiers: null);

            if (method is null)
                continue;

            try {
                method.Invoke(null, [SinkPluginRegistry.Instance]);
            } catch (TargetInvocationException ex) {
                errorStore.Add(new LogErrorEntry {
                    SinkName = "SourceGenerator",
                    ErrorMessage = $"Compile-time sink registration failed: {ex.InnerException?.Message ?? ex.Message}",
                    StackTrace = ex.InnerException?.StackTrace
                });
            }

            return;
        }
    }
}
