using System.Diagnostics.Metrics;

namespace CSharpEssentials.LoggerHelper.Telemetry.Metrics;
/// <summary>
/// A wrapper around <see cref="ObservableGauge{T}"/> that simplifies
/// creating an observable gauge instrument from a <see cref="Func{T}"/> provider.
/// </summary>
/// <typeparam name="T">
/// The numeric type of the gauge measurement (e.g., <see cref="double"/> or <see cref="long"/>).
/// Must be a value type (struct).
/// </typeparam>
public class GaugeWrapper<T> where T : struct {
    /// <summary>
    /// The delegate that provides the current value of the gauge when invoked.
    /// </summary>
    private readonly Func<T> _valueProvider;
    /// <summary>
    /// Holds the last value returned by the <see cref="_valueProvider"/>.
    /// Exposed publicly so callers can read the most recent measurement without 
    /// waiting for the next collection cycle.
    /// </summary>
    private T _lastValue;
    public T LastValue => _lastValue;
    /// <summary>
    /// The underlying <see cref="ObservableGauge{T}"/> instrument registered with the <see cref="Meter"/>.
    /// When the meter is collected, it invokes the callback provided in the constructor.
    /// </summary>
    public ObservableGauge<T> Gauge { get; }
    /// <summary>
    /// Constructs a new <see cref="GaugeWrapper{T}"/> that registers an observable gauge 
    /// with the specified <paramref name="meter"/>, <paramref name="name"/>, and metadata.
    /// </summary>
    /// <param name="meter">
    /// The <see cref="Meter"/> under which this gauge will be created.
    /// </param>
    /// <param name="name">
    /// The unique name of the metric (e.g., "memory_used_mb" or "postgresql.connections.active").
    /// </param>
    /// <param name="valueProvider">
    /// A <see cref="Func{T}"/> that returns the current value each time the gauge is collected.
    /// </param>
    /// <param name="unit">
    /// The unit of measurement for this gauge (e.g., "MB", "conn"). Optional; defaults to an empty string.
    /// </param>
    /// <param name="description">
    /// A human-readable description of what this gauge measures. Optional; defaults to an empty string.
    /// </param>
    public GaugeWrapper(Meter meter, string name, Func<T> valueProvider, string unit = "", string description = "") {
        _valueProvider = valueProvider;
        Gauge = meter.CreateObservableGauge(
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

