using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CSharpEssentials.LoggerHelper.Dashboard.Extensions;
using System.IO;
using System.Reflection;

public class ResourceManager {
    public string GetEmbeddedTextFile(string relativePath) {
        var assembly = Assembly.GetExecutingAssembly(); // O l'assembly dove sono incorporate le risorse

        // Il nome della risorsa segue una convenzione:
        // NamespaceDiDefault.Cartella.SottoCartella.NomeFile.Estensione
        // Esempio: Se il tuo namespace di default è "MyProject" e il file è MyResources\data.txt
        // il nome della risorsa sarà "MyProject.MyResources.data.txt"
        string resourceName = $"{assembly.GetName().Name}.{relativePath.Replace("/", ".").Replace("\\", ".")}";

        // Se non hai un namespace di default o vuoi essere più specifico
        // puoi elencare tutti i nomi delle risorse per trovarlo:
        // var allResourceNames = assembly.GetManifestResourceNames();
        // foreach (var name in allResourceNames) { Console.WriteLine(name); }

        using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
            if (stream == null) {
                Console.WriteLine($"Errore: Risorsa '{resourceName}' non trovata.");
                return null;
            }
            using (StreamReader reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }

    public byte[] GetEmbeddedBinaryFile(string relativePath) {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"{assembly.GetName().Name}.{relativePath.Replace("/", ".").Replace("\\", ".")}";

        using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
            if (stream == null) {
                Console.WriteLine($"Errore: Risorsa '{resourceName}' non trovata.");
                return null;
            }
            using (MemoryStream ms = new MemoryStream()) {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
public static class DashboardExtensions {
    public static void UseLoggerHelperDashboard(this WebApplication app, string path = "/loggerdashboard") {
        // Ottieni il nome completo della risorsa incorporata.
        // Questo dovrebbe includere il namespace radice del tuo progetto,
        // ad esempio "YourProjectNamespace.wwwroot.dashboard.index.html"
        // Assicurati che 'resourceName' corrisponda esattamente all'output di GetManifestResourceNames()
        var resourceName = "wwwroot.dashboard.index.html"; // <--- POTENZIALE PUNTO DI ERRORE
        var assembly = typeof(DashboardExtensions).Assembly;

        // Debug: Stampa tutti i nomi delle risorse incorporate per verificare
        // Console.WriteLine("Available resources in assembly:");
        // foreach (var res in assembly.GetManifestResourceNames())
        // {
        //     Console.WriteLine($"- {res}");
        // }
        // Mappatura per servire l'index.html
        app.MapGet(path, async context => {
            // Seleziona il nome della risorsa in base a ciò che hai trovato nel passo 1.
            // Esempio: "YourProjectNamespace.wwwroot.dashboard.index.html"
            // Se il nome nel file csproj è impostato tramite LogicalName, allora userai quello.
            // Altrimenti, usa il formato dedotto dal .NET SDK (RootNamespace + path).
            var actualResourceName = assembly.GetManifestResourceNames()
                                            .FirstOrDefault(r => r.EndsWith(".wwwroot.dashboard.index.html") || r.Equals("wwwroot.dashboard.index.html"));
            if (string.IsNullOrEmpty(actualResourceName)) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"Resource not found: {resourceName}. Please check if it's embedded correctly.");
                return;
            }
            using var stream = assembly.GetManifestResourceStream(actualResourceName);
            if (stream == null) {
                // Questo caso dovrebbe essere raro se actualResourceName è stato trovato,
                // ma è una buona safety net.
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"Resource not found (stream null): {actualResourceName}");
                return;
            }
            context.Response.ContentType = "text/html";
            await stream.CopyToAsync(context.Response.Body);
        });
    }
}
