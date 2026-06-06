using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Test endpoints for the File sink's FileNameProperty feature (v5.0.1).
/// Logs are routed to subdirectories based on the "TenantId" property value.
/// </summary>
public class DynamicFileEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/file-dynamic").WithTags("Dynamic File Routing");

        // GET /api/file-dynamic/tenant/{name}
        // Logs go to: Logs/{name}/log-.txt
        group.MapGet("/tenant/{name}", (string name, ILogger<DynamicFileEndpoints> logger) => {
            using (logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = name })) {
                logger.LogInformation("Request processed for tenant {TenantId}", name);
                logger.LogWarning("Slow query detected for tenant {TenantId}", name);
            }
            return Results.Ok(new {
                message = $"2 logs written for tenant '{name}'",
                expectedPath = $"Logs/{name}/log-*.txt"
            });
        })
        .WithSummary("Write 2 logs for a named tenant")
        .WithDescription(
            "Sets TenantId={name} in the log scope and emits one Information and one Warning log. " +
            "With FileNameProperty: 'TenantId' configured in the File sink, logs land in Logs/{name}/log-*.txt " +
            "instead of the base Logs/ path. " +
            "Try different names (e.g. /tenant/acme, /tenant/contoso) and use /verify to inspect the resulting folders.");

        // GET /api/file-dynamic/multi-tenant
        // Simulates 3 tenants writing logs in sequence
        group.MapGet("/multi-tenant", (ILogger<DynamicFileEndpoints> logger) => {
            var tenants = new[] { "acme", "contoso", "fabrikam" };
            foreach (var tenant in tenants) {
                using (logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = tenant })) {
                    logger.LogInformation("Order processed for {TenantId}", tenant);
                    logger.LogError("Payment failed for {TenantId}", tenant);
                }
            }
            return Results.Ok(new {
                message = "6 logs written across 3 tenants",
                expectedPaths = tenants.Select(t => $"Logs/{t}/log-*.txt").ToArray()
            });
        })
        .WithSummary("Write logs for 3 tenants in sequence")
        .WithDescription(
            "Iterates over acme, contoso, and fabrikam — each tenant gets 2 logs (Information + Error) " +
            "in its own isolated subdirectory. " +
            "Demonstrates that per-tenant file isolation works correctly when scopes are sequential. " +
            "Call /verify afterwards to see all three subdirectories created.");

        // GET /api/file-dynamic/no-tenant
        // Logs without TenantId — should go to base Logs/log-.txt
        group.MapGet("/no-tenant", (ILogger<DynamicFileEndpoints> logger) => {
            logger.LogInformation("This log has no TenantId — goes to base path");
            logger.LogWarning("Another log without TenantId");
            return Results.Ok(new {
                message = "2 logs written without TenantId",
                expectedPath = "Logs/log-*.txt (base path fallback)"
            });
        })
        .WithSummary("Write logs without TenantId — base path fallback")
        .WithDescription(
            "Emits 2 logs with no TenantId in scope. " +
            "When FileNameProperty is configured but absent from a log entry, the File sink falls back to the base path: Logs/log-*.txt. " +
            "Use /verify to confirm that these logs appear at the root and not in a subdirectory.");

        // GET /api/file-dynamic/mixed
        // Mix of tenant and no-tenant logs
        group.MapGet("/mixed", (ILogger<DynamicFileEndpoints> logger) => {
            logger.LogInformation("Global startup log — no tenant");

            using (logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = "acme" })) {
                logger.LogInformation("Acme order created");
                logger.LogError("Acme payment failed");
            }

            logger.LogWarning("Background job finished — no tenant");

            using (logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = "contoso" })) {
                logger.LogInformation("Contoso invoice generated");
            }

            return Results.Ok(new {
                message = "5 logs: 2 global (base path), 2 acme, 1 contoso",
                paths = new[] {
                    "Logs/log-*.txt (2 global logs)",
                    "Logs/acme/log-*.txt (2 acme logs)",
                    "Logs/contoso/log-*.txt (1 contoso log)"
                }
            });
        })
        .WithSummary("Mix tenant and non-tenant logs in one request")
        .WithDescription(
            "Writes 5 logs in a realistic pattern: 2 global logs (no scope → base path), " +
            "2 acme logs (Logs/acme/), and 1 contoso log (Logs/contoso/). " +
            "Models real-world scenarios where some code paths (startup, background jobs) " +
            "don't have a tenant context. Use /verify to confirm all three output locations.");

        // GET /api/file-dynamic/verify
        // Lists the actual log directories created
        group.MapGet("/verify", () => {
            var logDir = "Logs";
            if (!Directory.Exists(logDir))
                return Results.Ok(new { message = "Logs directory not found", directories = Array.Empty<string>() });

            var dirs = Directory.GetDirectories(logDir)
                .Select(d => new {
                    name = Path.GetFileName(d),
                    files = Directory.GetFiles(d, "*.txt").Select(Path.GetFileName).ToArray()
                }).ToList();

            var rootFiles = Directory.GetFiles(logDir, "*.txt")
                .Select(Path.GetFileName).ToArray();

            return Results.Ok(new {
                basePathFiles = rootFiles,
                subdirectories = dirs
            });
        })
        .WithSummary("List log directories and files on disk")
        .WithDescription(
            "Scans the Logs/ directory and returns all per-tenant subdirectories with their .txt filenames, " +
            "plus any .txt files at the root (non-tenant logs). " +
            "Run this after any of the other file-dynamic endpoints to confirm logs landed in the correct paths. " +
            "Returns an empty result if no logs have been written yet.");
    }
}
