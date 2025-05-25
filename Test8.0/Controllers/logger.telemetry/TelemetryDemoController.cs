using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Serilog.Events;
using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Telemetry;
using System.ComponentModel.DataAnnotations;

namespace Controllers.logger.telemetry;

[ApiController]
[Route("api/[controller]")]
public class LoggerHelperTelemetryController : ControllerBase {
    private static readonly ActivitySource ActivitySource = new("LoggerHelper");

    [HttpPost("crea")]
    public async Task<IActionResult> CreaOrdine() {
        //Ogni StartActivity crea un nuovo Span
        //Quindi Span potrebbe divetare la action concatenata con il metodo in cui ci troviamo !
        using var activity = ActivitySource.StartActivity("CreaOrdine");

        var ordine = new OrdineRequest { Username = "John", IpAddress = "127.0.0.1" };
        ordine.IdTransaction = activity.TraceId.ToString();
        ordine.Action = "CreaOrdine";

        activity?.SetTag("utente", "mario.rossi");
        activity?.SetTag("tipo_operazione", "Creazione ordine");
        activity?.SetTag("cliente_id", ordine.Username);

        loggerExtension<OrdineRequest>.TraceAsync(ordine, LogEventLevel.Information, null, "Avvio creazione ordine");

        try {
            // ✅ Sub-Span: Convalida
            using (var spanValidazione = ActivitySource.StartActivity("Convalida ordine")) {
                loggerExtension<OrdineRequest>.TraceAsync(ordine, LogEventLevel.Information, null, "Avvio validazione ordine");
                await Task.Delay(200); // Simula validazione
                loggerExtension<OrdineRequest>.TraceAsync(ordine, LogEventLevel.Information, null, "Ordine valido");
            }

            // ✅ Sub-Span: Salvataggio su DB
            using (var spanDb = ActivitySource.StartActivity("Salvataggio DB")) {
                loggerExtension<OrdineRequest>.TraceAsync(ordine, LogEventLevel.Information, null, "Inizio persistenza su database");
                await Task.Delay(350); // Simula lentezza DB
                loggerExtension<OrdineRequest>.TraceAsync(ordine, LogEventLevel.Information, null, "Ordine salvato correttamente");
            }

            loggerExtension<OrdineRequest>.TraceAsync(ordine, LogEventLevel.Information, null, "Creazione ordine completata con successo");
        } catch (Exception ex) {
            loggerExtension<OrdineRequest>.TraceAsync(ordine, LogEventLevel.Error, ex, "Errore nella creazione ordine");
            throw;
        }

        return Ok("Ordine creato");
    }
}

public class OrdineRequest : IRequest {
    public string IdTransaction { get; set; } = Guid.NewGuid().ToString();
    [Required(ErrorMessage = "Action is required")]
    public string Action { get; set; }
    public string ApplicationName { get; set; } = "Demo Telemetry";
    public string Username { get; set; }
    public string IpAddress { get; set; }
}