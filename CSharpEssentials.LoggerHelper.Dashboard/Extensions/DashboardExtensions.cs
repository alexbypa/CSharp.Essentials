using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace CSharpEssentials.LoggerHelper.Dashboard.Extensions;

public static class DashboardExtensions {
    // Metodo di estensione per registrare la dashboard embedded
    public static void UseLoggerHelperDashboard<T>(this WebApplication app, string path = "/loggerdashboard") where T : class , IRequest {
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


        app.MapGet("/ui/{**slug}", async context => {
            var assembly = typeof(DashboardExtensions).Assembly;
            var slug = context.Request.Path.Value?.Replace("/ui/", "").TrimStart('/');

            var resourcePrefix = "CSharpEssentials.LoggerHelper.Dashboard";
            var requestedResource = slug == "ui"
                ? assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("assets.index.html"))
                : $"{resourcePrefix}.{slug.Replace('/', '.')}";

            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("assets.index.html"));
            var stream = assembly.GetManifestResourceStream(requestedResource);
            if (stream == null) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"❌ Risorsa non trovata: {requestedResource}");
                return;
            }

            // Imposta Content-Type dinamicamente
            if (requestedResource.EndsWith(".html"))
                context.Response.ContentType = "text/html";
            else if (requestedResource.EndsWith(".js"))
                context.Response.ContentType = "application/javascript";
            else if (requestedResource.EndsWith(".css"))
                context.Response.ContentType = "text/css";
            else if (requestedResource.EndsWith(".svg"))
                context.Response.ContentType = "image/svg+xml";
            else
                context.Response.ContentType = "application/octet-stream";

            await stream.CopyToAsync(context.Response.Body);
        });

        app.MapGet("/api/logger-errors", () => {
            var errors = loggerExtension<T>.Errors;
            return Results.Ok(errors);
        });
    }
}