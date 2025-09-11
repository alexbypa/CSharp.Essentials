using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace CSharpEssentials.LoggerHelper.Dashboard.Extensions;

public static class DashboardExtensions {
    // Metodo di estensione per registrare la dashboard embedded
    public static void UseLoggerHelperDashboard<T>(this WebApplication app, string path = "/loggerdashboard") where T : class, IRequest {
        var assembly = typeof(DashboardExtensions).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        foreach (var name in resourceNames)
            Console.WriteLine(name);

        var assemblyForDebug = typeof(DashboardExtensions).Assembly;
        Console.WriteLine($"--- Debugging ManifestEmbeddedFileProvider Error ---");
        Console.WriteLine($"Assembly Name: {assemblyForDebug.FullName}"); // Controlla che sia l'assembly corretto

        var allResourceNames = assemblyForDebug.GetManifestResourceNames();
        Console.WriteLine($"Total embedded resources found: {allResourceNames.Length}");
        Console.WriteLine($"Looking for baseNamespace: 'CSharpEssentials.LoggerHelper.Dashboard'"); // Nota le virgolette per chiarezza

        bool foundAnyResourceWithPrefix = false;
        foreach (var name in allResourceNames) {
            Console.WriteLine($"  Found Resource: '{name}'"); // Stampa ogni nome tra virgolette per vedere spazi o caratteri extra
            if (name.StartsWith("CSharpEssentials.LoggerHelper.Dashboard")) // Case-sensitive check
            {
                foundAnyResourceWithPrefix = true;
            }
        }
        Console.WriteLine($"Found any resource starting with 'CSharpEssentials.LoggerHelper.Dashboard': {foundAnyResourceWithPrefix}");
        Console.WriteLine($"----------------------------------------------------");

        var testProvider = new ManifestEmbeddedFileProvider(assembly);
        var contents = testProvider.GetDirectoryContents("/");
        foreach (var file in contents)
            Console.WriteLine($">> Embedded file: {file.Name}");


        // Crea il FileProvider UNA VOLTA e riusalo
        var embeddedFileProvider = new ManifestEmbeddedFileProvider(
            assembly//,
                    //"assets" // **2. CORREZIONE FONDAMENTALE: baseNamespace**
        );

        // Questo middleware serve index.html quando l'URL termina con /ui/
        var defaultFilesOptions = new DefaultFilesOptions {
            FileProvider = embeddedFileProvider, // Usa lo stesso provider
            RequestPath = "/ui/assets", // Corrisponde al RequestPath di UseStaticFiles
            DefaultFileNames = { "index.html" } // "index.html" è ora alla "radice" del tuo baseNamespace appiattito
        };
        app.UseDefaultFiles(defaultFilesOptions);


        // 1) Handler condiviso
        RequestDelegate UiHandler = async context =>
        {
            var asm = typeof(DashboardExtensions).Assembly;
            var all = asm.GetManifestResourceNames();

            // ricava lo "slug" dal path (potrebbe essere vuoto se /ui)
            var raw = context.Request.Path.Value ?? "/ui";
            var slug = raw.StartsWith("/ui", StringComparison.OrdinalIgnoreCase)
                ? raw.Substring(3) // toglie "/ui"
                : raw;
            slug = slug.TrimStart('/');
            if (slug.Contains('?'))
                slug = slug.Split('?', 2)[0];

            const string nsRoot = "CSharpEssentials.LoggerHelper.Dashboard.";
            const string nsAssets = "CSharpEssentials.LoggerHelper.Dashboard.assets.";

            string? resourceName = null;

            if (string.IsNullOrEmpty(slug)) {
                resourceName = all.FirstOrDefault(r => r.EndsWith(".index.html", StringComparison.OrdinalIgnoreCase))
                            ?? (all.Contains(nsRoot + "index.html") ? nsRoot + "index.html" : null);
            } else {
                var dotted = slug.Replace('/', '.');
                var c1 = nsAssets + dotted;
                var c2 = nsRoot + dotted;

                if (all.Contains(c1))
                    resourceName = c1;
                else if (all.Contains(c2))
                    resourceName = c2;
                else
                    resourceName = all.FirstOrDefault(r => r.EndsWith("." + dotted, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrEmpty(resourceName)) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"❌ Embedded resource not found for slug '{slug}'.");
                return;
            }

            await using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"❌ Stream null for resource '{resourceName}'.");
                return;
            }

            context.Response.ContentType =
                resourceName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ? "text/html" :
                resourceName.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ? "application/javascript" :
                resourceName.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ? "text/css" :
                resourceName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? "image/svg+xml" :
                "application/octet-stream";

            await stream.CopyToAsync(context.Response.Body);
        };
        app.MapGet("/assets/{**slug}", async context =>
        {
            var asm = typeof(DashboardExtensions).Assembly;
            var all = asm.GetManifestResourceNames();

            const string nsAssets = "CSharpEssentials.LoggerHelper.Dashboard.assets.";

            var slug = (context.Request.Path.Value ?? "")
                        .Substring("/assets/".Length)
                        .TrimStart('/');
            var dotted = slug.Replace('/', '.');

            var resourceName = nsAssets + dotted;
            if (!all.Contains(resourceName))
                resourceName = all.FirstOrDefault(r => r.EndsWith("." + dotted, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(resourceName)) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"❌ Asset non trovato: {slug}");
                return;
            }

            await using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"❌ Stream nullo per: {resourceName}");
                return;
            }

            context.Response.ContentType =
                resourceName.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ? "application/javascript" :
                resourceName.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ? "text/css" :
                resourceName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? "image/svg+xml" :
                "application/octet-stream";

            await stream.CopyToAsync(context.Response.Body);
        });
        // 2) Route
        app.MapGet("/ui", UiHandler);
        app.MapGet("/ui/{**slug}", UiHandler);



        // Sostituisci questo mapping
        app.MapGet("/api/MonitorSink", () => {
            // ATTENZIONE: assicurati di avere il bridge del SelfLog attivo
            // (quello che copia i messaggi di SelfLog dentro GlobalLogger.Errors)
            // prima di chiamare questo endpoint.

            // Se nel tuo codice SinksLoaded ha proprietà SinkName/Levels, usa queste;
            // altrimenti adatta a Sink/Level.
            var sinks = GlobalLogger.SinksLoaded
                .Select(s => new LoggerSinkDto(
                    SinkName: s.SinkName,                  // se invece hai s.Sink, usa quello
                    Levels: (s.Levels ?? new List<string>()).ToList()   // se invece hai s.Level, usa quello
                ))
                .ToList();

            var errors = GlobalLogger.Errors
                .Select(e => new LoggerErrorDto(
                    Timestamp: e.Timestamp,
                    SinkName: e.SinkName,
                    ErrorMessage: e.ErrorMessage
                ))
                .OrderByDescending(e => e.Timestamp)
                .ToList();

            var dto = new LoggerReportDto(
                CurrentError: GlobalLogger.CurrentError,
                ErrorCount: errors.Count,
                SinkCount: sinks.Count,
                Sinks: sinks,
                Errors: errors
            );

            return Results.Ok(dto);
        });
    }
    // DTO minimali
    public record LoggerSinkDto(string SinkName, List<string> Levels);
    public record LoggerErrorDto(DateTime Timestamp, string SinkName, string ErrorMessage);

    public record LoggerReportDto(
        string? CurrentError,
        int ErrorCount,
        int SinkCount,
        List<LoggerSinkDto> Sinks,
        List<LoggerErrorDto> Errors
    );
}