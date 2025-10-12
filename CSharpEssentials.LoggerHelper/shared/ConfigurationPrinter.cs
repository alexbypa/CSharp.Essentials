using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.shared;

public static class ConfigurationPrinter {
    static RequestInfo request = new RequestInfo {
        Action = "ConfigurationPrinter",
        ApplicationName = "LoggerHelper Config"
    };
    public static void PrintByProvider(IConfiguration configuration) {
        var root = configuration as IConfigurationRoot
            ?? throw new InvalidOperationException("Passa un IConfigurationRoot.");

        // 1. Raccoglie i provider con eventuale nome file (per JSON)
        var providers = root.Providers.Select(p => {
            string display = p.GetType().Name;
            string file = (p is Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider jp)
                ? (jp.Source as Microsoft.Extensions.Configuration.Json.JsonConfigurationSource)?.Path
                : null;
            return new ProviderInfo(p, display, file);
        }).ToList();

        // 2. Tutte le chiavi presenti
        var allKeys = configuration.AsEnumerable()
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => kv.Key)
            .Distinct()
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        // 3. Per ogni provider, prendi solo le chiavi che effettivamente fornisce
        foreach (var providerInfo in providers.OrderBy(p => p.Display, StringComparer.Ordinal)) {
            var provider = providerInfo.Provider;
            var providedKeys = new List<(string Key, string Value)>();

            foreach (var key in allKeys) {
                if (provider.TryGet(key, out var value)) {
                    providedKeys.Add((key, value));
                }
            }

            if (providedKeys.Count == 0)
                continue;

            // 4. Stampa provider e coppie chiave/valore
            request.Action = $"Configuration from provider: {providerInfo.Display} {(string.IsNullOrEmpty(providerInfo.File) ? "(No File)" : $"(File: {providerInfo.File})")}";
            if (!string.IsNullOrEmpty(providerInfo.File))
                loggerExtension<RequestInfo>.TraceDashBoardSync(request, LogEventLevel.Information, null, $"  File: {providerInfo.File}");

            foreach (var (key, value) in providedKeys.OrderBy(kv => kv.Key, StringComparer.Ordinal)) {
                loggerExtension<RequestInfo>.TraceDashBoardSync(request, LogEventLevel.Information, null, $"  Key: {key} \n\rValue: {value}");
            }
        }
    }
    private record ProviderInfo(IConfigurationProvider Provider, string Display, string File);
}