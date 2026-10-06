using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpEssentials.LoggerHelper.Dashboard;

/// <summary>
/// Extension methods for wiring LoggerHelper's embedded dashboard into ASP.NET Core.
/// </summary>
public static class DashboardExtensions {
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>
    /// Registers dashboard services in the DI container.
    /// </summary>
    public static IServiceCollection AddLoggerHelperDashboard(this IServiceCollection services, Action<DashboardOptions>? configure = null) {
        var options = new DashboardOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddAuthorization();
        if (options.UsesBasicAuthentication) {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, DashboardBasicAuthenticationHandler>(DashboardBasicAuthenticationHandler.SchemeName, null);
        }
        return services;
    }

    /// <summary>
    /// Maps the dashboard endpoints: HTML UI, JSON API, and SSE log stream.
    /// All endpoints require authentication: HTTP Basic (<see cref="DashboardOptions.UseBasicAuthentication"/>),
    /// otherwise the app's default authentication scheme, plus the optional <see cref="DashboardOptions.AuthorizationPolicy"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <c>AddLoggerHelperDashboard</c> was not called, the policy does not exist, or no authentication is configured.
    /// </exception>
    public static IEndpointRouteBuilder MapLoggerHelperDashboard(this IEndpointRouteBuilder endpoints) {
        var dashOpts = endpoints.ServiceProvider.GetService<DashboardOptions>()
            ?? throw new InvalidOperationException("DashboardOptions not registered: call AddLoggerHelperDashboard(...) before MapLoggerHelperDashboard().");
        var basePath = dashOpts.Path.TrimEnd('/');
        EnsureAuthenticationConfigured(endpoints.ServiceProvider, dashOpts);
        var group = endpoints.MapGroup(basePath);

        // Main dashboard HTML page
        group.MapGet("", (HttpContext ctx) => {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            return ctx.Response.WriteAsync(DashboardHtml.Render(basePath, dashOpts.RefreshIntervalSeconds));
        });

        // JSON API: sink status
        group.MapGet("/api/status", (
            LoggerHelperOptions options,
            ILogErrorStore errorStore,
            ILoadedSinkStore sinkStore,
            IServiceProvider sp) => {
                var sinks = sinkStore.GetAll();
                var errors = errorStore.GetAll();
                var errorCount = errorStore.Count;
                var buffer = sp.GetService<ContextualLogBuffer>();
                var lastFlush = buffer?.LastFlush;
                return Results.Json(new {
                    health = errorCount == 0 ? "OK" : errorCount < 10 ? "WARNING" : "CRITICAL",
                    applicationName = options.ApplicationName,
                    contextualLogging = options.General.EnableContextualLogging,
                    masking = options.SensitiveDataMasking.Enabled,
                    sinks = sinks.Select(s => new {
                        name = s.SinkName,
                        status = s.Configured ? "ACTIVE" : "FAILED",
                        levels = s.Levels,
                        pluginType = s.PluginType
                    }),
                    routes = options.Routes.Select(r => new {
                        sink = r.Sink,
                        levels = r.Levels
                    }),
                    errors = new {
                        total = errorCount,
                        recent = errors.TakeLast(20).Select(e => new {
                            timestamp = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                            sink = e.SinkName,
                            message = e.ErrorMessage,
                            context = e.ContextInfo,
                            stackTrace = e.StackTrace
                        })
                    },
                    lastFlush = lastFlush is null ? null : new {
                        flushedAt = lastFlush.FlushedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        triggeringError = lastFlush.TriggeringError is null ? null : new {
                            timestamp = lastFlush.TriggeringError.Timestamp.ToString("HH:mm:ss.fff"),
                            level = lastFlush.TriggeringError.Level.ToString(),
                            source = lastFlush.TriggeringError.SourceContext,
                            message = lastFlush.TriggeringError.Message
                        },
                        entries = lastFlush.Entries.Select(e => new {
                            timestamp = e.Timestamp.ToString("HH:mm:ss.fff"),
                            level = e.Level.ToString(),
                            source = e.SourceContext,
                            message = e.Message
                        })
                    }
                }, _jsonOptions);
            });

        // JSON API: recent logs from context buffer
        group.MapGet("/api/logs", (
            HttpContext ctx,
            IServiceProvider sp) => {
                var buffer = sp.GetService<ContextualLogBuffer>();
                if (buffer is null)
                    return Results.Json(new { enabled = false, message = "Contextual logging is not enabled" }, _jsonOptions);

                var levelFilter = ctx.Request.Query["level"].FirstOrDefault();
                var queryFilter = ctx.Request.Query["query"].FirstOrDefault();

                var entries = buffer.Snapshot();
                IEnumerable<LogBufferEntry> filtered = entries;

                if (!string.IsNullOrEmpty(levelFilter) && Enum.TryParse<Serilog.Events.LogEventLevel>(levelFilter, true, out var lvl))
                    filtered = filtered.Where(e => e.Level == lvl);
                if (!string.IsNullOrEmpty(queryFilter))
                    filtered = filtered.Where(e =>
                        (e.Message?.Contains(queryFilter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (e.SourceContext?.Contains(queryFilter, StringComparison.OrdinalIgnoreCase) ?? false));

                return Results.Json(new {
                    enabled = true,
                    capacity = buffer.Capacity,
                    count = buffer.Count,
                    entries = filtered.Select(e => new {
                        timestamp = e.Timestamp.ToString("HH:mm:ss.fff"),
                        level = e.Level.ToString(),
                        source = e.SourceContext,
                        message = e.Message
                    })
                }, _jsonOptions);
            });

        // SSE: live log stream
        group.MapGet("/api/stream", async (HttpContext ctx, IServiceProvider sp, CancellationToken ct) => {
            var buffer = sp.GetService<ContextualLogBuffer>();
            if (buffer is null) {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync("Contextual logging is not enabled", ct);
                return;
            }

            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            try {
                var lastPush = buffer.TotalPushes;
                while (!ct.IsCancellationRequested) {
                    var currentPush = buffer.TotalPushes;
                    if (currentPush > lastPush) {
                        var newCount = (int)Math.Min(currentPush - lastPush, buffer.Capacity);
                        var snapshot = buffer.Snapshot();
                        var newEntries = snapshot.Count > newCount ? snapshot.Skip(snapshot.Count - newCount) : snapshot;
                        foreach (var entry in newEntries) {
                            var data = JsonSerializer.Serialize(new {
                                timestamp = entry.Timestamp.ToString("HH:mm:ss.fff"),
                                level = entry.Level.ToString(),
                                source = entry.SourceContext,
                                message = entry.Message
                            }, _jsonOptions);
                            await ctx.Response.WriteAsync($"data: {data}\n\n", ct);
                        }
                        await ctx.Response.Body.FlushAsync(ct);
                        lastPush = currentPush;
                    }
                    await Task.Delay(1000, ct);
                }
            } catch (OperationCanceledException) {
                // Client disconnected — normal for SSE
            }
        });

        if (dashOpts.UsesBasicAuthentication) {
            group.RequireAuthorization(new AuthorizationPolicyBuilder(DashboardBasicAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()
                .Build());
            if (!string.IsNullOrWhiteSpace(dashOpts.AuthorizationPolicy))
                group.RequireAuthorization(dashOpts.AuthorizationPolicy);
        } else if (!string.IsNullOrWhiteSpace(dashOpts.AuthorizationPolicy)) {
            group.RequireAuthorization(dashOpts.AuthorizationPolicy);
        } else {
            group.RequireAuthorization();
        }

        return endpoints;
    }

    private static void EnsureAuthenticationConfigured(IServiceProvider sp, DashboardOptions options) {
        // Startup-only: provider tasks are already completed, blocking is safe.
        var hasPolicy = !string.IsNullOrWhiteSpace(options.AuthorizationPolicy);
        if (hasPolicy) {
            var policy = sp.GetRequiredService<IAuthorizationPolicyProvider>()
                .GetPolicyAsync(options.AuthorizationPolicy!).GetAwaiter().GetResult();
            if (policy is null)
                throw new InvalidOperationException($"LoggerHelper Dashboard: authorization policy '{options.AuthorizationPolicy}' not found.");
        }

        if (options.UsesBasicAuthentication || hasPolicy)
            return;

        var scheme = sp.GetService<IAuthenticationSchemeProvider>()?.GetDefaultChallengeSchemeAsync().GetAwaiter().GetResult();
        if (scheme is null)
            throw new InvalidOperationException("LoggerHelper Dashboard requires authentication: configure AddAuthentication(...) with a default scheme, or call options.UseBasicAuthentication(user, password) in AddLoggerHelperDashboard.");
    }
}