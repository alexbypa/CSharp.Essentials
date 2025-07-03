using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.IO;

namespace CSharpEssentials.LoggerHelper.Dashboard.Extensions;

public static class ResourceHelper {
    public static Stream GetResourceStream(string resourceName) {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{resourceName}");
    }
    public static string[] GetResourceNames() {
        return Assembly.GetExecutingAssembly().GetManifestResourceNames();
    }
}
public static class DashboardExtensions {
    // Metodo di estensione per registrare la dashboard embedded
    public static void UseLoggerHelperDashboard(this WebApplication app, string path = "/loggerdashboard") {
        // Ottiene il riferimento all'assembly dove sono incluse le risorse embed (es. index.html)
        var assembly = typeof(DashboardExtensions).Assembly;

        // Cerca tra tutte le risorse embedded quella che termina con 'wwwroot.dashboard.index.html'
        // Il nome deve combaciare con il LogicalName definito nel .csproj
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("wwwroot.dashboard.index.html"));

        // Gestisce il path principale (es. /loggerdashboard)
        app.MapGet(path, async context => {
            // Se la risorsa embedded non è trovata, restituisce 404 con messaggio d'errore
            if (string.IsNullOrEmpty(resourceName)) {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("❌ index.html not found!");
                return;
            }

            // Apre lo stream del file index.html dalla risorsa embedded
            using var stream = assembly.GetManifestResourceStream(resourceName);

            // Imposta il Content-Type della risposta HTTP su "text/html"
            context.Response.ContentType = "text/html";

            // Copia il contenuto della risorsa nel corpo della risposta (restituisce index.html)
            await stream.CopyToAsync(context.Response.Body);
        });

        app.MapFallback(context =>
        {
            var path = context.Request.Path.Value;
            //if (path != null && (path.EndsWith(".js") || path.EndsWith(".css") || path.Contains("/static/"))) {
            //    // Non fare fallback! lascia che UseStaticFiles gestisca
            //    context.Response.StatusCode = 404;
            //    return context.Response.WriteAsync($"Static file not found: {path}");
            //}

            context.Response.ContentType = "text/html";
            var assembly = typeof(DashboardExtensions).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("wwwroot.dashboard.index.html"));

            if (string.IsNullOrEmpty(resourceName)) {
                context.Response.StatusCode = 404;
                return context.Response.WriteAsync("index.html not found");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            return stream.CopyToAsync(context.Response.Body);
        });
    }
}
