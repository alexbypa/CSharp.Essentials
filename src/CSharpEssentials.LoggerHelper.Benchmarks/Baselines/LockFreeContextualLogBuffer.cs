using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Benchmarks.Baselines;

/// <summary>
/// Pre-A54 copy of the ring buffer (commit 68d4500^), kept for comparison only. NOT thread-safe:
/// it has known races (torn entries, count/head skew during flush). Do not use outside benchmarks.
/// </summary>
/// <remarks>
/// Original summary: zero-allocation ring buffer that retains the last N log events.
/// On error/fatal, flushes context to sinks with IsContextualHistory flag.
/// Thread-safe via Interlocked — no locks, no allocations after startup.
/// </remarks>
internal sealed class LockFreeContextualLogBuffer {
    private readonly LockFreeLogBufferEntry[] _buffer;
    private long _head; // next write position (Interlocked)
    private int _count;
    private readonly int _capacity;
    private volatile LockFreeContextFlushEvent? _lastFlush;

    internal LockFreeContextualLogBuffer(int capacity = 100) {
        _capacity = capacity;
        _buffer = new LockFreeLogBufferEntry[capacity];
        for (int i = 0; i < capacity; i++)
            _buffer[i] = new LockFreeLogBufferEntry();
    }

    /// <summary>
    /// Records a log event into the ring buffer. Lock-free, O(1).
    /// </summary>
    internal void Push(LogEventLevel level, string message, string? sourceContext, DateTime timestamp) {
        var index = (int)(Interlocked.Increment(ref _head) % _capacity);
        if (index < 0)
            index += _capacity;

        var entry = _buffer[index];
        entry.Level = level;
        entry.Message = message;
        entry.SourceContext = sourceContext;
        entry.Timestamp = timestamp;
        entry.IsOccupied = true;

        // Track count up to capacity
        int current;
        do {
            current = _count;
            if (current >= _capacity)
                break;
        } while (Interlocked.CompareExchange(ref _count, current + 1, current) != current);
    }

    /// <summary>
    /// Returns all buffered entries in chronological order and clears the buffer.
    /// Called when an error/fatal is detected to flush context.
    /// Pass <paramref name="triggeringError"/> to record the event that caused the flush —
    /// stored separately in <see cref="LockFreeContextFlushEvent.TriggeringError"/> so the
    /// Dashboard can display it as the cause, visually distinct from the preceding context.
    /// </summary>
    internal IReadOnlyList<LockFreeLogBufferEntry> FlushAndClear(LockFreeLogBufferEntry? triggeringError = null) {
        var snapshot = new List<LockFreeLogBufferEntry>();
        var currentHead = (int)(_head % _capacity);
        if (currentHead < 0)
            currentHead += _capacity;
        var currentCount = Math.Min(_count, _capacity);

        // Read in chronological order (oldest first)
        for (int i = 0; i < currentCount; i++) {
            var idx = (currentHead - currentCount + 1 + i + _capacity) % _capacity;
            var entry = _buffer[idx];
            if (entry.IsOccupied) {
                snapshot.Add(new LockFreeLogBufferEntry {
                    Level = entry.Level,
                    Message = entry.Message,
                    SourceContext = entry.SourceContext,
                    Timestamp = entry.Timestamp,
                    IsOccupied = true
                });
                entry.IsOccupied = false;
                entry.Message = null;
                entry.SourceContext = null;
            }
        }

        _count = 0;
        if (snapshot.Count > 0 || triggeringError is not null)
            _lastFlush = new LockFreeContextFlushEvent(DateTime.UtcNow, snapshot, triggeringError);
        return snapshot;
    }

    /// <summary>
    /// The most recent flush event (context entries captured before the last error).
    /// Null if no error has triggered a flush yet. Used by the Dashboard.
    /// </summary>
    internal LockFreeContextFlushEvent? LastFlush => _lastFlush;

    /// <summary>
    /// Returns a snapshot of buffered entries in chronological order without clearing.
    /// Used by MCP search tool and Dashboard.
    /// </summary>
    internal IReadOnlyList<LockFreeLogBufferEntry> Snapshot() {
        var result = new List<LockFreeLogBufferEntry>();
        var currentHead = (int)(Volatile.Read(ref _head) % _capacity);
        if (currentHead < 0)
            currentHead += _capacity;
        var currentCount = Math.Min(Volatile.Read(ref _count), _capacity);

        for (int i = 0; i < currentCount; i++) {
            var idx = (currentHead - currentCount + 1 + i + _capacity) % _capacity;
            var entry = _buffer[idx];
            if (entry.IsOccupied) {
                result.Add(new LockFreeLogBufferEntry {
                    Level = entry.Level,
                    Message = entry.Message,
                    SourceContext = entry.SourceContext,
                    Timestamp = entry.Timestamp,
                    IsOccupied = true
                });
            }
        }
        return result;
    }

    internal int Count => Math.Min(_count, _capacity);
    internal int Capacity => _capacity;

    /// <summary>
    /// Monotonically increasing counter of total pushes. Used by SSE stream
    /// to detect new entries even after the ring buffer wraps around.
    /// </summary>
    internal long TotalPushes => Volatile.Read(ref _head);
}

/// <summary>
/// Pre-allocated entry in the ring buffer. Mutable by design to avoid allocations.
/// </summary>
internal sealed class LockFreeLogBufferEntry {
    internal LogEventLevel Level { get; set; }
    internal string? Message { get; set; }
    internal string? SourceContext { get; set; }
    internal DateTime Timestamp { get; set; }
    internal bool IsOccupied { get; set; }
}

/// <summary>
/// Immutable record of a context flush event — the entries that were in the ring buffer
/// when an error/fatal was detected, plus the timestamp of the flush.
/// <see cref="TriggeringError"/> holds the Error/Fatal event that caused the flush,
/// stored separately so the Dashboard can display it as the cause.
/// </summary>
internal sealed class LockFreeContextFlushEvent {
    internal DateTime FlushedAt { get; }
    internal IReadOnlyList<LockFreeLogBufferEntry> Entries { get; }

    /// <summary>
    /// The Error or Fatal event that triggered this flush. Never buffered in the ring
    /// (captured levels are Debug/Info/Warning only), so stored here explicitly.
    /// </summary>
    internal LockFreeLogBufferEntry? TriggeringError { get; }

    internal LockFreeContextFlushEvent(DateTime flushedAt, IReadOnlyList<LockFreeLogBufferEntry> entries, LockFreeLogBufferEntry? triggeringError = null) {
        FlushedAt = flushedAt;
        Entries = entries;
        TriggeringError = triggeringError;
    }
}