using System.Diagnostics.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry;
public class GaugeWrapper<T> where T : struct {
    private readonly Func<T> _valueProvider;
    private T _lastValue;
    public T LastValue => _lastValue;

    public ObservableGauge<T> Gauge { get; }

    public GaugeWrapper(Meter meter, string name, Func<T> valueProvider, string unit = "", string description = "") {
        _valueProvider = valueProvider;

        Gauge = meter.CreateObservableGauge<T>(
            name,
            () => {
                _lastValue = _valueProvider();
                return new Measurement<T>(_lastValue);
            },
            unit,
            description
        );
    }
}

