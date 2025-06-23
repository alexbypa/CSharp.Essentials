using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace CSharpEssentials.LoggerHelper.Telemetry.middlewares;
/// <summary>
/// Questo startup filter si occupa di inserire TraceIdPropagationMiddleware
/// all'inizio della pipeline HTTP, in modo che venga eseguito per ogni richiesta.
/// </summary>
internal class TraceIdPropagationStartupFilter : IStartupFilter {
    /// <summary>
    /// Questo metodo viene chiamato automaticamente all'avvio dell'applicazione.
    /// L'argomento `next` è un delegate che rappresenta la pipeline esistente.
    /// Restituiamo un nuovo delegate che prima registra il middleware,
    /// poi richiama la pipeline successiva.
    /// </summary>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) {
        return builder => {
            // 1) Inserisco il tuo TraceIdPropagationMiddleware
            builder.UseMiddleware<TraceIdPropagationMiddleware>();

            // 2) Richiamo la pipeline precedente (altri middleware / endpoint)
            next(builder);
        };
    }
}