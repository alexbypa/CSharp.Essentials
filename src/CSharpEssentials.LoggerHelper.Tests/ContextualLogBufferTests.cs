using Xunit.Abstractions;
using System.Reflection;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public sealed class ContextualLogBufferTests {
    private readonly ITestOutputHelper _output;

    public ContextualLogBufferTests(ITestOutputHelper output) {
        _output = output;
    }

    private static void PushN(ContextualLogBuffer buffer, int from, int to) {
        for (var i = from; i <= to; i++)
            buffer.Push(LogEventLevel.Information, $"m{i}", $"ctx{i}", DateTime.UtcNow);
    }

    private static string[] Messages(IReadOnlyList<LogBufferEntry> entries) =>
        entries.Select(e => e.Message!).ToArray();

    [Fact]
    public void Constructor_DefaultCapacity_Is100AndEmpty() {
        _output.WriteLine("[Scenario] Creo un ContextualLogBuffer senza specificare la capacità");
        _output.WriteLine("[Atteso] Capacity == 100, Count == 0, Snapshot vuoto e LastFlush null");

        var buffer = new ContextualLogBuffer();

        _output.WriteLine($"[Restituito] Capacity={buffer.Capacity}, Count={buffer.Count}, Snapshot={buffer.Snapshot().Count}, LastFlush={(buffer.LastFlush is null ? "null" : "presente")}");
        Assert.Equal(100, buffer.Capacity);
        Assert.Equal(0, buffer.Count);
        Assert.Empty(buffer.Snapshot());
        Assert.Null(buffer.LastFlush);
    }

    [Fact]
    public void Snapshot_PushedBelowCapacity_ReturnsChronologicalOrder() {
        _output.WriteLine("[Scenario] Buffer da 5 posti con 3 messaggi (m1..m3) inseriti: leggo lo Snapshot");
        _output.WriteLine("[Atteso] Snapshot in ordine cronologico [m1, m2, m3] e Count == 3");

        var buffer = new ContextualLogBuffer(5);
        PushN(buffer, 1, 3);

        var snapshot = buffer.Snapshot();

        _output.WriteLine($"[Restituito] Snapshot=[{string.Join(", ", Messages(snapshot))}], Count={buffer.Count}");
        Assert.Equal(["m1", "m2", "m3"], Messages(snapshot));
        Assert.Equal(3, buffer.Count);
    }

    [Fact]
    public void Snapshot_PushedBeyondCapacity_KeepsLastNInOrder() {
        _output.WriteLine("[Scenario] Buffer da 3 posti con 5 messaggi (m1..m5) inseriti: il buffer circolare sovrascrive i più vecchi");
        _output.WriteLine("[Atteso] Snapshot contiene solo gli ultimi 3 [m3, m4, m5] in ordine; Count == 3 e TotalPushes == 5");

        var buffer = new ContextualLogBuffer(3);
        PushN(buffer, 1, 5);

        var snapshot = buffer.Snapshot();

        _output.WriteLine($"[Restituito] Snapshot=[{string.Join(", ", Messages(snapshot))}], Count={buffer.Count}, TotalPushes={buffer.TotalPushes}");
        Assert.Equal(["m3", "m4", "m5"], Messages(snapshot));
        Assert.Equal(3, buffer.Count);
        Assert.Equal(5, buffer.TotalPushes);
    }

    [Fact]
    public void Snapshot_Called_DoesNotClear() {
        _output.WriteLine("[Scenario] Buffer con 2 messaggi: chiamo Snapshot (lettura) e poi controllo di nuovo lo stato");
        _output.WriteLine("[Atteso] Snapshot non svuota il buffer: Count == 2 e un secondo Snapshot restituisce ancora 2 elementi");

        var buffer = new ContextualLogBuffer(5);
        PushN(buffer, 1, 2);

        _ = buffer.Snapshot();

        _output.WriteLine($"[Restituito] Count={buffer.Count}, SecondSnapshot={buffer.Snapshot().Count}");
        Assert.Equal(2, buffer.Count);
        Assert.Equal(2, buffer.Snapshot().Count);
    }

    [Fact]
    public void FlushAndClear_WithEntries_ReturnsChronologicalAndEmptiesBuffer() {
        _output.WriteLine("[Scenario] Buffer da 5 posti con 3 messaggi (m1..m3): chiamo FlushAndClear");
        _output.WriteLine("[Atteso] Restituisce [m1, m2, m3] in ordine cronologico e il buffer rimane vuoto (Count == 0, Snapshot vuoto)");

        var buffer = new ContextualLogBuffer(5);
        PushN(buffer, 1, 3);

        var flushed = buffer.FlushAndClear();

        _output.WriteLine($"[Restituito] Flushed=[{string.Join(", ", Messages(flushed))}], Count={buffer.Count}, Snapshot={buffer.Snapshot().Count}");
        Assert.Equal(["m1", "m2", "m3"], Messages(flushed));
        Assert.Equal(0, buffer.Count);
        Assert.Empty(buffer.Snapshot());
    }

    [Fact]
    public void FlushAndClear_AfterFlush_ReleasesMessageAndSourceContext() {
        _output.WriteLine("[Scenario] Buffer da 4 posti riempito oltre capacità (6 messaggi), poi FlushAndClear; ispeziono via reflection l'array interno _buffer");
        _output.WriteLine("[Atteso] Memory leak check: in ogni slot Message e SourceContext sono null (le stringhe non restano referenziate dopo il flush)");

        var buffer = new ContextualLogBuffer(4);
        PushN(buffer, 1, 6);

        buffer.FlushAndClear();

        var field = typeof(ContextualLogBuffer).GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var slots = (LogBufferEntry[])field.GetValue(buffer)!;
        _output.WriteLine($"[Restituito] Slot={slots.Length}, SlotConStringhe={slots.Count(s => s.Message is not null || s.SourceContext is not null)}");
        Assert.All(slots, s => {
            Assert.Null(s.Message);
            Assert.Null(s.SourceContext);
        });
    }

    [Fact]
    public void FlushAndClear_AfterWrapAround_ReturnsLastNInOrder() {
        _output.WriteLine("[Scenario] Buffer da 3 posti con 7 messaggi (m1..m7): il buffer ha fatto più giri (wrap-around), poi FlushAndClear");
        _output.WriteLine("[Atteso] Restituisce solo gli ultimi 3 messaggi in ordine cronologico [m5, m6, m7]");

        var buffer = new ContextualLogBuffer(3);
        PushN(buffer, 1, 7);

        var flushed = buffer.FlushAndClear();

        _output.WriteLine($"[Restituito] Flushed=[{string.Join(", ", Messages(flushed))}]");
        Assert.Equal(["m5", "m6", "m7"], Messages(flushed));
    }

    [Fact]
    public void FlushAndClear_WithEntries_SetsLastFlushWithEntriesAndTriggeringError() {
        _output.WriteLine("[Scenario] Buffer con 2 messaggi (m1, m2): FlushAndClear passando un LogBufferEntry Error \"boom\" come errore scatenante");
        _output.WriteLine("[Atteso] LastFlush valorizzato con TriggeringError uguale all'istanza passata, Entries [m1, m2] e FlushedAt recente");

        var buffer = new ContextualLogBuffer(5);
        PushN(buffer, 1, 2);
        var error = new LogBufferEntry { Level = LogEventLevel.Error, Message = "boom" };

        buffer.FlushAndClear(error);

        var last = buffer.LastFlush;
        _output.WriteLine($"[Restituito] TriggeringError={last?.TriggeringError?.Message}, Entries=[{string.Join(", ", Messages(last!.Entries))}], FlushedAt={last.FlushedAt:O}");
        Assert.NotNull(last);
        Assert.Same(error, last.TriggeringError);
        Assert.Equal(["m1", "m2"], Messages(last.Entries));
        Assert.True((DateTime.UtcNow - last.FlushedAt).Duration() < TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void FlushAndClear_EmptyWithoutTrigger_DoesNotSetLastFlush() {
        _output.WriteLine("[Scenario] Buffer vuoto: FlushAndClear senza errore scatenante");
        _output.WriteLine("[Atteso] Nessun elemento restituito e LastFlush resta null (nulla da tracciare)");

        var buffer = new ContextualLogBuffer(5);

        var flushed = buffer.FlushAndClear();

        _output.WriteLine($"[Restituito] Flushed={flushed.Count}, LastFlush={(buffer.LastFlush is null ? "null" : "presente")}");
        Assert.Empty(flushed);
        Assert.Null(buffer.LastFlush);
    }

    [Fact]
    public void FlushAndClear_EmptyWithTrigger_SetsLastFlushWithEmptyEntries() {
        _output.WriteLine("[Scenario] Buffer vuoto: FlushAndClear con un errore scatenante di livello Fatal");
        _output.WriteLine("[Atteso] LastFlush valorizzato con Entries vuote e TriggeringError uguale all'istanza passata");

        var buffer = new ContextualLogBuffer(5);
        var error = new LogBufferEntry { Level = LogEventLevel.Fatal, Message = "boom" };

        buffer.FlushAndClear(error);

        _output.WriteLine($"[Restituito] LastFlush={(buffer.LastFlush is null ? "null" : "presente")}, Entries={buffer.LastFlush?.Entries.Count}, TriggeringError={buffer.LastFlush?.TriggeringError?.Level}");
        Assert.NotNull(buffer.LastFlush);
        Assert.Empty(buffer.LastFlush.Entries);
        Assert.Same(error, buffer.LastFlush.TriggeringError);
    }

    [Fact]
    public void Push_AfterFlush_StartsFreshInOrder() {
        _output.WriteLine("[Scenario] Buffer da 3 posti riempito (m1..m4), svuotato con FlushAndClear, poi inserisco m5 e m6");
        _output.WriteLine("[Atteso] Il buffer riparte da zero: Snapshot [m5, m6] e Count == 2 (nessun residuo del giro precedente)");

        var buffer = new ContextualLogBuffer(3);
        PushN(buffer, 1, 4);
        buffer.FlushAndClear();

        PushN(buffer, 5, 6);

        _output.WriteLine($"[Restituito] Snapshot=[{string.Join(", ", Messages(buffer.Snapshot()))}], Count={buffer.Count}");
        Assert.Equal(["m5", "m6"], Messages(buffer.Snapshot()));
        Assert.Equal(2, buffer.Count);
    }

    [Fact]
    public void Push_Concurrent_CountNeverExceedsCapacityAndNoException() {
        _output.WriteLine("[Scenario] Buffer da 64 posti: 10.000 Push concorrenti con Parallel.For");
        _output.WriteLine("[Atteso] Nessuna eccezione; Count == 64 (mai oltre la capacità), TotalPushes == 10000 e Snapshot di 64 elementi");

        var buffer = new ContextualLogBuffer(64);

        Parallel.For(0, 10_000, i =>
            buffer.Push(LogEventLevel.Debug, "m" + i, "ctx", DateTime.UtcNow));

        _output.WriteLine($"[Restituito] Count={buffer.Count}, TotalPushes={buffer.TotalPushes}, Snapshot={buffer.Snapshot().Count}");
        Assert.Equal(64, buffer.Count);
        Assert.Equal(10_000, buffer.TotalPushes);
        Assert.Equal(64, buffer.Snapshot().Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveCapacity_ThrowsArgumentOutOfRange(int capacity) {
        _output.WriteLine($"[Scenario] Creo un ContextualLogBuffer con capacità non positiva ({capacity})");
        _output.WriteLine("[Atteso] ArgumentOutOfRangeException");

        var ex = Record.Exception(() => new ContextualLogBuffer(capacity));

        _output.WriteLine($"[Restituito] {ex?.GetType().Name}");
        Assert.IsType<ArgumentOutOfRangeException>(ex);
    }

    [Fact]
    public async Task PushAndFlush_Concurrent_NoLossNoDuplicates() {
        _output.WriteLine("[Scenario] Buffer da 100.000 posti (non fa wrap): 20.000 Push unici concorrenti mentre un task esegue FlushAndClear in loop, poi flush finale");
        _output.WriteLine("[Atteso] L'unione dei flush contiene esattamente 20.000 messaggi, senza duplicati e senza Message null");

        const int n = 20_000;
        var buffer = new ContextualLogBuffer(100_000);
        var collected = new List<string>();
        var pushing = true;

        var flusher = Task.Run(() => {
            while (Volatile.Read(ref pushing)) {
                foreach (var e in buffer.FlushAndClear())
                    collected.Add(e.Message!);
            }
        });

        Parallel.For(0, n, i =>
            buffer.Push(LogEventLevel.Information, "m" + i, "ctx", DateTime.UtcNow));
        Volatile.Write(ref pushing, false);
        await flusher;
        foreach (var e in buffer.FlushAndClear())
            collected.Add(e.Message!);

        _output.WriteLine($"[Restituito] Totale={collected.Count}, Distinti={collected.Distinct().Count()}");
        Assert.Equal(n, collected.Count);
        Assert.Equal(n, collected.Distinct().Count());
        Assert.DoesNotContain(null!, collected);
    }

    [Fact]
    public void FlushAndClear_ConcurrentFlushes_NoDuplicates() {
        _output.WriteLine("[Scenario] Buffer da 1.000 posti pieno: 8 FlushAndClear in parallelo");
        _output.WriteLine("[Atteso] Il totale restituito è esattamente 1.000 e ogni messaggio compare una sola volta");

        const int cap = 1_000;
        var buffer = new ContextualLogBuffer(cap);
        PushN(buffer, 1, cap);

        var results = new IReadOnlyList<LogBufferEntry>[8];
        Parallel.For(0, 8, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => results[i] = buffer.FlushAndClear());

        var all = results.SelectMany(r => r.Select(e => e.Message!)).ToList();
        _output.WriteLine($"[Restituito] Totale={all.Count}, Distinti={all.Distinct().Count()}");
        Assert.Equal(cap, all.Count);
        Assert.Equal(cap, all.Distinct().Count());
    }
}
