using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace CSharpEssentials.LoggerHelper.Telemetry.middlewares;
/// <summary>
/// Startup filter that ensures the TraceIdPropagationMiddleware is inserted
/// at the beginning of the HTTP pipeline, so that each request receives a trace ID.
/// </summary>
internal class TraceIdPropagationStartupFilter : IStartupFilter {
    /// <summary>
    /// This method is called automatically during application startup.
    /// The 'next' parameter is a delegate that represents the rest of the middleware pipeline.
    /// We return a new delegate that first registers our middleware, then continues the pipeline.
    /// </summary>
    /// <param name="next">Delegate to the next middleware registration phase</param>
    /// <returns>A wrapped Action<IApplicationBuilder> that registers our middleware first</returns>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) {
        return builder => {
            // 1) Inserisco il tuo TraceIdPropagationMiddleware
            builder.UseMiddleware<TraceIdPropagationMiddleware>();

            // 2) Richiamo la pipeline precedente (altri middleware / endpoint)
            next(builder);
        };
    }
}