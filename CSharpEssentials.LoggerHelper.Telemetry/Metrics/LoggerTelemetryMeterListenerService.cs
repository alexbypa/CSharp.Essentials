using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using CSharpEssentials.LoggerHelper.Telemetry.Proxy;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Metrics;

public class LoggerTelemetryMeterListenerService : BackgroundService {
    private readonly IMetricEntryFactory _factory;
    private readonly IMetricEntryRepository _repository;
    ITelemetryGatekeeper _gatekeeper;
    private readonly List<MetricEntry> _buffer = new();
    private readonly object _lock = new();
    private MeterListener? _listener;

    public LoggerTelemetryMeterListenerService(
        IMetricEntryFactory factory,
        IMetricEntryRepository repository,
        ITelemetryGatekeeper gatekeeper

    ) {
        _factory = factory;
        _repository = repository;
        _gatekeeper = gatekeeper;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_gatekeeper.IsEnabled)
            return;

        _listener = new MeterListener {
            InstrumentPublished = (instrument, listener) =>
            {
                Console.WriteLine($"Published: Meter={instrument.Meter.Name}, Instrument={instrument.Name}, Type={instrument.GetType().Name}");
                // In debug: abilita tutto. Poi potrai filtrare.
                listener.EnableMeasurementEvents(instrument);
            }
        };

        // ✅ callback per tutti i tipi che usi
        _listener.SetMeasurementEventCallback<double>(OnMeasurement);
        _listener.SetMeasurementEventCallback<long>((i, m, t, s) => OnMeasurement(i, (double)m, t, s));
        _listener.SetMeasurementEventCallback<int>((i, m, t, s) => OnMeasurement(i, (double)m, t, s));
        _listener.SetMeasurementEventCallback<float>((i, m, t, s) => OnMeasurement(i, (double)m, t, s));
        _listener.SetMeasurementEventCallback<decimal>((i, m, t, s) => OnMeasurement(i, (double)m, t, s));

        _listener.Start();

        try {
            // loop principale del servizio
            while (!stoppingToken.IsCancellationRequested) {
                // 🔑 interroga gli ObservableGauge (telemetry.parallel_*, memory_used_mb, ecc.)
                _listener.RecordObservableInstruments();

                // frequenza di campionamento (regolabile)
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                // scrive su DB il buffer accumulato
                await FlushBufferAsync(stoppingToken);
            }
        } finally {
            // ultimo giro per non perdere misure
            _listener.RecordObservableInstruments();
            await FlushBufferAsync(CancellationToken.None);
            _listener?.Dispose();
        }
    }

    private void OnMeasurement(Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) {
        var entry = _factory.Create(instrument, measurement, tags);
        lock (_lock) {
            try {
            _buffer.Add(entry);
            }catch (Exception ex) {
                Console.WriteLine($"ERR Buffer Metric: {ex.Message}");
            }
        }
    }
    private async Task FlushBufferAsync(CancellationToken token) {
        List<MetricEntry> toWrite;
        lock (_lock) {
            if (_buffer.Count == 0)
                return;
            toWrite = new List<MetricEntry>(_buffer);
            _buffer.Clear();
        }
        await _repository.SaveAsync(toWrite, token);
    }
    public override void Dispose() {
        if (_gatekeeper.IsEnabled) {
            _listener?.Dispose();
            base.Dispose();
        }
    }
}