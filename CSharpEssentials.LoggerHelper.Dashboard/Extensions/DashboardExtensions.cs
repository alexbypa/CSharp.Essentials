using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace CSharpEssentials.LoggerHelper.Dashboard.Extensions;

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

        // Questo MapFallbackToFile potrebbe essere problematico o ridondante se MapGet gestisce già la rotta principale.
        // Se la tua dashboard React ha routing lato client, potresti volerlo solo
        // se /loggerdashboard è la base e tutte le altre rotte interne
        // (es. /loggerdashboard/details) dovrebbero servire index.html.
        // Per ora, concentriamoci sulla rotta principale.
        // app.MapFallbackToFile("index.html"); // Considera di commentarlo o spostarlo
    }
}
//public static class DashboardExtensions {
//    public static void UseLoggerHelperDashboard(this WebApplication app, string path = "/loggerdashboard") {
//        var resourceName = "wwwroot.dashboard.index.html";
//        var assembly = typeof(DashboardExtensions).Assembly;


//        app.MapFallbackToFile("index.html");

//        app.MapGet(path, async context => {
//            using var stream = assembly.GetManifestResourceStream(resourceName);
//            if (stream == null) {
//                context.Response.StatusCode = 404;
//                await context.Response.WriteAsync($"❌ Resource not found: {resourceName}");
//                return;
//            }

//            context.Response.ContentType = "text/html";
//            await stream.CopyToAsync(context.Response.Body);
//        });

//    }
//    //public static void UseLoggerHelperDashboard(this WebApplication app, string path = "/loggerdashboard") {

//    //    var embeddedProvider = new EmbeddedFileProvider(typeof(DashboardExtensions).Assembly);

//    //    app.UseFileServer(new FileServerOptions {
//    //        RequestPath = path,
//    //        FileProvider = embeddedProvider,
//    //        EnableDefaultFiles = true
//    //    });

//    //    var res = typeof(DashboardExtensions).Assembly.GetManifestResourceNames();
//    //    foreach (var name in res)
//    //        Console.WriteLine($"MANIFEST NAME: {name}");

//    //    var files = typeof(DashboardExtensions).Assembly.GetFiles();
//    //    foreach (var file in files)
//    //        Console.WriteLine($"FILE: {file.Name}. Length: {file.Length} ");

//    //    app.MapGet("/loggerdashboard", async context =>
//    //    {
//    //        var resourceName = "wwwroot.dashboard.index.html";
//    //        var stream = typeof(DashboardExtensions).Assembly.GetManifestResourceStream(resourceName);

//    //        if (stream == null) {
//    //            context.Response.StatusCode = 404;
//    //            await context.Response.WriteAsync($"❌ Resource not found: {resourceName}");
//    //            return;
//    //        }

//    //        context.Response.ContentType = "text/html";
//    //        await stream.CopyToAsync(context.Response.Body);
//    //    });
//    //    // Serve tutte le sotto-route
//    //    app.MapFallback($"{path}/{{*path}}", async context => {
//    //        var file = embeddedProvider.GetFileInfo("wwwroot/dashboard/index.html");
//    //        if (!file.Exists) {
//    //            await context.Response.WriteAsync("❌ embeddedProvider: file does NOT exist");
//    //            return;
//    //        }

//    //        await context.Response.WriteAsync("❌ embeddedProvider: file does NOT exist");
//    //        context.Response.ContentType = "text/html";
//    //        await context.Response.SendFileAsync(file); 
//    //    });


//    //    // questi verranno recuperati dagli altri packages ( telemetry e loggerHelper.Errors )
//    //    // Endpoint API dashboard
//    //    app.MapGet($"{path}/api/logs", async ([FromServices] TelemetriesDbContext db) =>
//    //        await db.LogEntry.OrderByDescending(l => l.raise_date).Take(50).ToListAsync());

//    //    app.MapGet($"{path}/api/metrics", async ([FromServices] TelemetriesDbContext db) =>
//    //        await db.Metrics.OrderByDescending(m => m.Timestamp).Take(100).ToListAsync());
//    //}
//}
