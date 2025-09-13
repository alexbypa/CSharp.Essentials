//TODO: implement authentication/authorization

//using CSharpEssentials.LoggerHelper.Telemetry.EF.Data;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using System.Text.Json;

//namespace CSharpEssentials.LoggerHelper.Telemetry.Configuration;
//public class DbOptionsMonitor<T> : IOptionsMonitor<T> where T : class, new() {
//    private readonly IServiceProvider _sp;
//    private T _currentValue;
//    private readonly List<Action<T, string>> _listeners = new();
//    private CancellationTokenSource _cts = new();
//    private string _name;

//    public DbOptionsMonitor(IServiceProvider sp, string name = "") {
//        _sp = sp;
//        _name = name;
//        _currentValue = LoadFromDb();

//        StartPolling();
//    }

//    public T CurrentValue => _currentValue;

//    public T Get(string name) => _currentValue;

//    public IDisposable OnChange(Action<T, string> listener) {
//        _listeners.Add(listener);
//        return new ChangeTracker(() => _listeners.Remove(listener));
//    }

//    private void StartPolling() {
//        _ = Task.Run(async () => {
//            while (!_cts.Token.IsCancellationRequested) {
//                await Task.Delay(TimeSpan.FromSeconds(20), _cts.Token);

//                var latest = LoadFromDb();
//                if (!JsonSerializer.Serialize(_currentValue).Equals(JsonSerializer.Serialize(latest))) {
//                    _currentValue = latest;
//                    foreach (var listener in _listeners)
//                        listener(_currentValue, _name);
//                }
//            }
//        });
//    }
//    private T LoadFromDb() {
//        using var scope = _sp.CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<TelemetriesDbContext>();

//        var config = db.LoggerTelemetryOptions.FirstOrDefault();
//        if (config == null)
//            return new T();

//        return new LoggerTelemetryOptions {
//            IsEnabled = config.IsEnabled,
//            MeterListenerIsEnabled = config.MeterListenerIsEnabled,
//            ConnectionString = config.ConnectionString
//        } as T ?? new T();
//    }

//    private class ChangeTracker : IDisposable {
//        private readonly Action _onDispose;
//        public ChangeTracker(Action onDispose) => _onDispose = onDispose;
//        public void Dispose() => _onDispose();
//    }
//}