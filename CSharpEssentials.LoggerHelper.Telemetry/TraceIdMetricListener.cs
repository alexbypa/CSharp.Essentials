using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry;

public static class TraceIdMetricListener {
    public static void Register() {
        var listener = new MeterListener();

        listener.InstrumentPublished = (instrument, listenerHandle) => {
            // Filtra solo le metriche auto-strumentate (puoi raffinare)
            if (instrument.Meter.Name == "Microsoft.AspNetCore") {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) => {
            if (Activity.Current is not { } activity)
                return;

            // Ricostruzione dei tag con trace_id incluso
            var enrichedTags = tags.ToArray().ToList();
            enrichedTags.Add(new("trace_id", activity.TraceId.ToString()));

            // Salva i tag nel contesto globale (es. se hai un exporter che legge da qui)
            // oppure loggali
            Console.WriteLine($"📊 Metric: {instrument.Name}, trace_id={activity.TraceId}, value={value}");

            // Qui non si può riscrivere la metrica: solo leggere e loggare.
            // Se usi OTLP exporter, i tag arricchiti devono essere già in `Activity`.
        });

        listener.Start();
    }
}
