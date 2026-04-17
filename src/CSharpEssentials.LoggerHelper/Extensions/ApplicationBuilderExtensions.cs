using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Extension methods for IApplicationBuilder to add LoggerHelper middleware.
/// </summary>
public static class ApplicationBuilderExtensions {
    /// <summary>
    /// Adds the request/response logging middleware if enabled in options.
    ///
    /// Example:
    ///   app.UseLoggerHelper();
    /// </summary>
    public static IApplicationBuilder UseLoggerHelper(this IApplicationBuilder app) {
        var options = app.ApplicationServices.GetRequiredService<LoggerHelperOptions>();
        if (options.General.EnableRequestResponseLogging)
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
        return app;
    }
}
