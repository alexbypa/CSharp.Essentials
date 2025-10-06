using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Telemetry.shared;
public class ConfigurationPrinter {
    private readonly IConfiguration _configuration;
    private readonly IConfigurationRoot _configurationRoot;

    public ConfigurationPrinter(IConfiguration configuration) {
        _configuration = configuration;
        _configurationRoot = configuration as IConfigurationRoot;
    }

    public void PrintAllConfigurationWithSources() {
        Console.WriteLine("=== CONFIGURAZIONE COMPLETA CON SORGENTI ===\n");

        // Ottenere tutti i provider di configurazione
        if (_configurationRoot != null) {
            Console.WriteLine("Provider di configurazione caricati:");
            foreach (var provider in _configurationRoot.Providers) {
                Console.WriteLine($"  - {provider.GetType().Name}");

                // Per JSON file providers, mostrare il path del file
                if (provider is Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider jsonProvider) {
                    var source = jsonProvider.Source as Microsoft.Extensions.Configuration.Json.JsonConfigurationSource;
                    Console.WriteLine($"    File: {source?.Path}");
                }
            }
            Console.WriteLine();
        }

        // Stampare tutte le chiavi e valori
        Console.WriteLine("Tutte le configurazioni:");
        PrintConfigurationRecursive(_configuration.AsEnumerable());

        Console.WriteLine("\n=== DETTAGLIO SORGENTI PER CHIAVE ===\n");
        PrintConfigurationWithSourceDetails();
    }

    private void PrintConfigurationRecursive(IEnumerable<KeyValuePair<string, string>> config) {
        foreach (var kvp in config.OrderBy(x => x.Key)) {
            if (!string.IsNullOrEmpty(kvp.Value)) {
                Console.WriteLine($"{kvp.Key} = {kvp.Value}");
            }
        }
    }

    private void PrintConfigurationWithSourceDetails() {
        if (_configurationRoot == null) {
            Console.WriteLine("IConfigurationRoot non disponibile. Iniettare IConfigurationRoot invece di IConfiguration.");
            return;
        }

        var allKeys = _configuration.AsEnumerable()
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x => x.Key)
            .OrderBy(x => x);

        foreach (var key in allKeys) {
            Console.WriteLine($"\nChiave: {key}");
            Console.WriteLine($"Valore: {_configuration[key]}");

            // Trovare quale provider fornisce questo valore
            var providers = _configurationRoot.Providers.Reverse(); // Reverse perché l'ultimo vince

            foreach (var provider in providers) {
                if (provider.TryGet(key, out string value)) {
                    var providerType = provider.GetType().Name;
                    var fileName = "N/A";

                    if (provider is Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider jsonProvider) {
                        var source = jsonProvider.Source as Microsoft.Extensions.Configuration.Json.JsonConfigurationSource;
                        fileName = source?.Path ?? "Unknown";
                    }

                    Console.WriteLine($"  ✓ Fornito da: {providerType}");
                    if (fileName != "N/A") {
                        Console.WriteLine($"    File: {fileName}");
                    }
                    Console.WriteLine($"    Valore: {value}");
                    break; // Mostra solo il provider "vincente"
                }
            }
        }
    }

    // Metodo alternativo più compatto
    public void PrintConfigurationSummary() {
        Console.WriteLine("=== RIEPILOGO CONFIGURAZIONE ===\n");

        var groupedBySource = new Dictionary<string, List<string>>();

        if (_configurationRoot != null) {
            var allKeys = _configuration.AsEnumerable()
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .Select(x => x.Key);

            foreach (var key in allKeys) {
                foreach (var provider in _configurationRoot.Providers.Reverse()) {
                    if (provider.TryGet(key, out _)) {
                        var fileName = "Other";

                        if (provider is Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider jsonProvider) {
                            var source = jsonProvider.Source as Microsoft.Extensions.Configuration.Json.JsonConfigurationSource;
                            fileName = source?.Path ?? "Unknown JSON";
                        } else {
                            fileName = provider.GetType().Name;
                        }

                        if (!groupedBySource.ContainsKey(fileName)) {
                            groupedBySource[fileName] = new List<string>();
                        }
                        groupedBySource[fileName].Add($"{key} = {_configuration[key]}");
                        break;
                    }
                }
            }

            foreach (var source in groupedBySource.OrderBy(x => x.Key)) {
                Console.WriteLine($"\n📄 {source.Key}");
                Console.WriteLine(new string('-', 50));
                foreach (var config in source.Value) {
                    Console.WriteLine($"  {config}");
                }
            }
        }
    }
}