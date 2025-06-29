using CSharpEssentials.LoggerHelper.Telemetry.EF.Models;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Metrics;

public class LoggerTelemetryMeterListenerService : BackgroundService {
    private readonly IMetricEntryFactory _factory;
    private readonly IMetricEntryRepository _repository;
    private readonly List<MetricEntry> _buffer = new();
    private readonly object _lock = new();
    private MeterListener? _listener;

    public LoggerTelemetryMeterListenerService(
        IMetricEntryFactory factory,
        IMetricEntryRepository repository
    ) {
        _factory = factory;
        _repository = repository;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        _listener = new MeterListener {
            //TOHACK: passare i parametri esternamente tramite IOptions !
            InstrumentPublished = (instrument, listener) => {
                if (instrument.Meter.Name == "LoggerHelper.Metrics"
                    || instrument.Name.StartsWith("db.client")
                    || instrument.Name.StartsWith("http.server")) {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        _listener.SetMeasurementEventCallback<double>(OnMeasurement);
        _listener.Start();

        _ = Task.Run(async () => {
            while (!stoppingToken.IsCancellationRequested) {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                await FlushBufferAsync(stoppingToken);
            }
            await FlushBufferAsync(stoppingToken);
        }, stoppingToken);

        return Task.CompletedTask;
    }

    private void OnMeasurement(Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) {
        var entry = _factory.Create(instrument, measurement, tags);
        lock (_lock) {
            _buffer.Add(entry);
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
        _listener?.Dispose();
        base.Dispose();
    }
}