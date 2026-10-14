using Xunit.Abstractions;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace CSharpEssentials.LoggerHelper.Tests;

public class RequestResponseLoggingMiddlewareTests {
    private readonly ITestOutputHelper _output;

    public RequestResponseLoggingMiddlewareTests(ITestOutputHelper output) {
        _output = output;
    }

    private static DefaultHttpContext CreateContext(string path, string query, string body = "") {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";
        ctx.Request.Path = path;
        ctx.Request.QueryString = new QueryString(query);
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task InvokeAsync_QueryWithEncodedCrLf_LogsSanitizedQuery() {
        _output.WriteLine("[Scenario] POST /api con query string \"?q=a%0D%0Ab\" (CR/LF percent-encoded, rischio log forging)");
        _output.WriteLine("[Atteso] Il middleware logga QueryString sanitizzata \"?q=a b\" e nessun valore stringa loggato contiene CR");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(CreateContext("/api", "?q=a%0D%0Ab"));

        var query = Assert.IsType<string>(logger.Single().Values["QueryString"]);
        _output.WriteLine($"[Restituito] QueryString=\"{query}\"");
        Assert.Equal("?q=a b", query);
        Assert.All(logger.Single().Values.Values.OfType<string>(), v => Assert.DoesNotContain('\r', v));
    }

    [Fact]
    public async Task InvokeAsync_RequestAndResponseBodyWithCrLf_LogsSanitizedBodies() {
        _output.WriteLine("[Scenario] Request body \"line1\\r\\nline2\" e response body \"r1\\r\\nr2\" (CR/LF reali nei body)");
        _output.WriteLine("[Atteso] RequestBody loggato \"line1 line2\" e ResponseBody loggato \"r1 r2\"");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(
            ctx => ctx.Response.WriteAsync("r1\r\nr2"), logger);

        await middleware.InvokeAsync(CreateContext("/api", "", "line1\r\nline2"));

        var values = logger.Single().Values;
        _output.WriteLine($"[Restituito] RequestBody=\"{values["RequestBody"]}\", ResponseBody=\"{values["ResponseBody"]}\"");
        Assert.Equal("line1 line2", values["RequestBody"]);
        Assert.Equal("r1 r2", values["ResponseBody"]);
    }

    [Theory]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    [InlineData("\f")]
    public async Task InvokeAsync_BodyWithUnicodeLineSeparator_LogsSanitizedBody(string sep) {
        _output.WriteLine($"[Scenario] Path, request body e response body contengono il separatore di riga Unicode U+{(int)sep[0]:X4}");
        _output.WriteLine("[Atteso] Path \"/x y\", RequestBody \"a b\" e ResponseBody \"a b\": il separatore diventa uno spazio");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(
            ctx => ctx.Response.WriteAsync("a" + sep + "b"), logger);

        await middleware.InvokeAsync(CreateContext("/x" + sep + "y", "", "a" + sep + "b"));

        var values = logger.Single().Values;
        _output.WriteLine($"[Restituito] Path=\"{values["Path"]}\", RequestBody=\"{values["RequestBody"]}\", ResponseBody=\"{values["ResponseBody"]}\"");
        Assert.Equal("/x y", values["Path"]);
        Assert.Equal("a b", values["RequestBody"]);
        Assert.Equal("a b", values["ResponseBody"]);
    }

    [Fact]
    public async Task InvokeAsync_BodyWithLoneCr_ReplacedWithSpace() {
        _output.WriteLine("[Scenario] Request body \"a\\rb\" con un CR isolato (senza LF)");
        _output.WriteLine("[Atteso] RequestBody loggato \"a b\": anche il CR isolato viene sostituito da spazio");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(CreateContext("/api", "", "a\rb"));

        _output.WriteLine($"[Restituito] RequestBody=\"{logger.Single().Values["RequestBody"]}\"");
        Assert.Equal("a b", logger.Single().Values["RequestBody"]);
    }

    [Fact]
    public async Task InvokeAsync_EmptyBodies_LogsEmptyPlaceholder() {
        _output.WriteLine("[Scenario] Richiesta senza body e risposta senza body");
        _output.WriteLine("[Atteso] RequestBody e ResponseBody loggati come \"(empty)\" con livello Information");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(CreateContext("/api", ""));

        var values = logger.Single().Values;
        _output.WriteLine($"[Restituito] RequestBody=\"{values["RequestBody"]}\", ResponseBody=\"{values["ResponseBody"]}\", Level={logger.Single().Level}");
        Assert.Equal("(empty)", values["RequestBody"]);
        Assert.Equal("(empty)", values["ResponseBody"]);
        Assert.Equal(LogLevel.Information, logger.Single().Level);
    }

    [Fact]
    public async Task InvokeAsync_NextThrows_LogsSanitizedPathAndRethrows() {
        _output.WriteLine("[Scenario] Il middleware successivo lancia InvalidOperationException(\"boom\") su un path con CR/LF (\"/evil\\r\\npath\")");
        _output.WriteLine("[Atteso] L'eccezione viene rilanciata; un log di livello Error con eccezione e Path sanitizzato \"/evil path\"");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(
            _ => throw new InvalidOperationException("boom"), logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(CreateContext("/evil\r\npath", "")));

        var entry = logger.Single();
        _output.WriteLine($"[Restituito] Level={entry.Level}, Exception={entry.Exception?.GetType().Name}, Path=\"{entry.Values["Path"]}\"");
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.IsType<InvalidOperationException>(entry.Exception);
        Assert.Equal("/evil path", entry.Values["Path"]);
    }

    [Fact]
    public async Task InvokeAsync_EventStreamAccept_SkipsLogging() {
        _output.WriteLine("[Scenario] Richiesta con header Accept \"text/event-stream\" (SSE, stream di lunga durata)");
        _output.WriteLine("[Atteso] Il middleware successivo viene comunque chiamato ma non viene scritto alcun log (evita di bufferizzare lo stream)");

        var logger = new CapturingLogger();
        var called = false;
        var middleware = new RequestResponseLoggingMiddleware(_ => { called = true; return Task.CompletedTask; }, logger);
        var ctx = CreateContext("/sse", "");
        ctx.Request.Headers.Accept = "text/event-stream";

        await middleware.InvokeAsync(ctx);

        _output.WriteLine($"[Restituito] NextChiamato={called}, LogEntries={logger.Entries.Count}");
        Assert.True(called);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task InvokeAsync_ResponseWritten_CopiesBodyToOriginalStream() {
        _output.WriteLine("[Scenario] Il middleware successivo scrive \"hello\" nella response");
        _output.WriteLine("[Atteso] Il body bufferizzato viene copiato nello stream originale: contiene \"hello\"");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(ctx => ctx.Response.WriteAsync("hello"), logger);
        var ctx = CreateContext("/api", "");
        var original = (MemoryStream)ctx.Response.Body;

        await middleware.InvokeAsync(ctx);

        _output.WriteLine($"[Restituito] StreamOriginale=\"{Encoding.UTF8.GetString(original.ToArray())}\"");
        Assert.Equal("hello", Encoding.UTF8.GetString(original.ToArray()));
    }

    [Fact]
    public async Task InvokeAsync_NextReadsRequestBody_BodyStillReadableDownstream() {
        _output.WriteLine("[Scenario] Request body \"payload\": il middleware successivo legge l'intero body");
        _output.WriteLine("[Atteso] Il body resta leggibile a valle (\"payload\") e viene loggato come RequestBody \"payload\"");

        var logger = new CapturingLogger();
        string? downstream = null;
        var middleware = new RequestResponseLoggingMiddleware(async ctx => {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            downstream = await reader.ReadToEndAsync();
        }, logger);

        await middleware.InvokeAsync(CreateContext("/api", "", "payload"));

        _output.WriteLine($"[Restituito] LettoAValle=\"{downstream}\", RequestBody=\"{logger.Single().Values["RequestBody"]}\"");
        Assert.Equal("payload", downstream);
        Assert.Equal("payload", logger.Single().Values["RequestBody"]);
    }

    [Theory]
    [InlineData(400, LogLevel.Error)]
    [InlineData(404, LogLevel.Error)]
    [InlineData(500, LogLevel.Error)]
    [InlineData(399, LogLevel.Information)]
    public async Task InvokeAsync_StatusCodeAtLeast400_LogsError(int status, LogLevel expected) {
        _output.WriteLine($"[Scenario] Il middleware successivo imposta lo status code {status}");
        _output.WriteLine($"[Atteso] Livello di log {expected} (>= 400 => Error, altrimenti Information)");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(ctx => { ctx.Response.StatusCode = status; return Task.CompletedTask; }, logger);

        await middleware.InvokeAsync(CreateContext("/api", ""));

        _output.WriteLine($"[Restituito] Level={logger.Single().Level}");
        Assert.Equal(expected, logger.Single().Level);
    }

    [Fact]
    public async Task InvokeAsync_Completed_RestoresOriginalResponseBody() {
        _output.WriteLine("[Scenario] Il middleware successivo scrive \"x\" e la richiesta si completa con successo");
        _output.WriteLine("[Atteso] Context.Response.Body torna a essere lo stream originale (non resta lo stream di buffering)");

        var middleware = new RequestResponseLoggingMiddleware(ctx => ctx.Response.WriteAsync("x"), new CapturingLogger());
        var ctx = CreateContext("/api", "");
        var original = ctx.Response.Body;

        await middleware.InvokeAsync(ctx);

        _output.WriteLine($"[Restituito] StessoStream={ReferenceEquals(original, ctx.Response.Body)}");
        Assert.Same(original, ctx.Response.Body);
    }

    [Fact]
    public async Task InvokeAsync_NextThrowsAfterWritingNotStarted_DiscardsPartialAndRethrows() {
        _output.WriteLine("[Scenario] Il middleware successivo scrive \"partial\" e poi lancia, con la response non ancora iniziata (HasStarted=false)");
        _output.WriteLine("[Atteso] Eccezione rilanciata; il parziale viene scartato (stream originale vuoto), lo stream originale è ripristinato e viene loggato un Error");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(async ctx => {
            await ctx.Response.WriteAsync("partial");
            throw new InvalidOperationException("boom");
        }, logger);
        var ctx = CreateContext("/api", "");
        var original = (MemoryStream)ctx.Response.Body;

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(ctx));
        _output.WriteLine($"[Restituito] LunghezzaStreamOriginale={original.Length}, StreamRipristinato={ReferenceEquals(original, ctx.Response.Body)}, LogError={logger.Entries.Any(e => e.Level == LogLevel.Error)}");

        Assert.Equal(0, original.Length);
        Assert.Same(original, ctx.Response.Body);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_NextThrowsAfterWritingStarted_CopiesPartialAndRethrows() {
        _output.WriteLine("[Scenario] Il middleware successivo scrive \"partial\" e poi lancia, con la response già iniziata (HasStarted=true)");
        _output.WriteLine("[Atteso] Eccezione rilanciata; il parziale già inviato viene copiato nello stream originale (\"partial\") e viene loggato un Error");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(async ctx => {
            await ctx.Response.WriteAsync("partial");
            throw new InvalidOperationException("boom");
        }, logger);
        var ctx = CreateContext("/api", "");
        ctx.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        var original = (MemoryStream)ctx.Response.Body;

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(ctx));
        _output.WriteLine($"[Restituito] StreamOriginale=\"{Encoding.UTF8.GetString(original.ToArray())}\", LogError={logger.Entries.Any(e => e.Level == LogLevel.Error)}");

        Assert.Equal("partial", Encoding.UTF8.GetString(original.ToArray()));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_NextThrowsAndCopyFails_RethrowsOriginalException() {
        _output.WriteLine("[Scenario] Il middleware successivo scrive \"partial\" e lancia InvalidOperationException(\"boom\"); la response è già iniziata e lo stream di destinazione lancia IOException(\"client gone\") durante la copia");
        _output.WriteLine("[Atteso] Viene rilanciata l'eccezione originale (InvalidOperationException) e non quella secondaria di copia (IOException)");

        var middleware = new RequestResponseLoggingMiddleware(async ctx => {
            await ctx.Response.WriteAsync("partial");
            throw new InvalidOperationException("boom");
        }, new CapturingLogger());
        var ctx = CreateContext("/api", "");
        ctx.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        ctx.Response.Body = new ThrowingWriteStream();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(ctx));

        _output.WriteLine($"[Restituito] Eccezione rilanciata: {ex.GetType().Name}(\"{ex.Message}\")");
        Assert.Equal("boom", ex.Message);
    }

    private sealed class StartedResponseFeature : HttpResponseFeature {
        public override bool HasStarted => true;
    }

    [Fact]
    public async Task InvokeAsync_RequestBodyTruncatedAtSurrogatePair_DoesNotSplitPair() {
        _output.WriteLine("[Scenario] Request body da 65.535 \"a\" seguiti da una emoji (surrogate pair) e da \"tail\": il limite di troncamento cade a metà della coppia");
        _output.WriteLine("[Atteso] Il body loggato termina con \"... (truncated)\", ha lunghezza 65.535 + marcatore e non contiene mezza coppia surrogata (high surrogate orfano)");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);
        var ctx = CreateContext("/api", "");
        // High surrogate lands on char index 65535 (last kept char), low surrogate on 65536.
        var text = new string('a', 65_535) + "😀" + "tail";
        ctx.Request.Body = new ChunkedStream(Encoding.UTF8.GetBytes(text), 4096, throwOnSyncRead: true);

        try {
            await middleware.InvokeAsync(ctx);
        } finally {
            await ctx.Request.Body.DisposeAsync();
        }

        var body = Assert.IsType<string>(logger.Single().Values["RequestBody"]);
        _output.WriteLine($"[Restituito] Lunghezza={body.Length}, UltimoCarattereTenutoHighSurrogate={char.IsHighSurrogate(body[65_534])}, Fine=\"{body[^15..]}\"");
        Assert.EndsWith("... (truncated)", body);
        Assert.Equal(65_535 + "... (truncated)".Length, body.Length);
        Assert.False(char.IsHighSurrogate(body[65_534]));
    }

    [Theory]
    [InlineData(65_536, false)]
    [InlineData(65_537, true)]
    public async Task InvokeAsync_RequestBodyAtLimit_TruncatesOnlyAboveMaxChars(int chars, bool truncated) {
        _output.WriteLine($"[Scenario] Request body non-seekable di {chars} caratteri (limite di logging: 65.536)");
        _output.WriteLine($"[Atteso] Troncato == {truncated}: solo oltre 65.536 caratteri il body loggato termina con \"... (truncated)\"");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);
        var ctx = CreateContext("/api", "");
        ctx.Request.Body = new ChunkedStream(new byte[chars].Select(_ => (byte)'a').ToArray(), 4096, throwOnSyncRead: true);

        try {
            await middleware.InvokeAsync(ctx);
        } finally {
            await ctx.Request.Body.DisposeAsync();
        }

        var body = Assert.IsType<string>(logger.Single().Values["RequestBody"]);
        _output.WriteLine($"[Restituito] Troncato={body.EndsWith("... (truncated)")}, Lunghezza={body.Length}");
        Assert.Equal(truncated, body.EndsWith("... (truncated)"));
    }

    private sealed class ThrowingWriteStream : MemoryStream {
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new IOException("client gone");
        public override void Write(ReadOnlySpan<byte> buffer) =>
            throw new IOException("client gone");
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            throw new IOException("client gone");
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new IOException("client gone");
    }

    [Fact]
    public async Task InvokeAsync_NextThrows_RestoresOriginalResponseBody() {
        _output.WriteLine("[Scenario] Il middleware successivo lancia InvalidOperationException(\"boom\") senza aver scritto nulla");
        _output.WriteLine("[Atteso] Eccezione rilanciata e Response.Body ripristinato allo stream originale (finally)");

        var middleware = new RequestResponseLoggingMiddleware(_ => throw new InvalidOperationException("boom"), new CapturingLogger());
        var ctx = CreateContext("/api", "");
        var original = ctx.Response.Body;

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(ctx));
        _output.WriteLine($"[Restituito] StessoStream={ReferenceEquals(original, ctx.Response.Body)}");

        Assert.Same(original, ctx.Response.Body);
    }

    [Fact]
    public async Task InvokeAsync_RequestBodyArrivesInSmallChunks_LogsFullBody() {
        _output.WriteLine("[Scenario] Request body da 5 KB consegnato in chunk da 100 byte (lettura async non-seekable)");
        _output.WriteLine("[Atteso] Il body loggato è completo e identico all'originale (nessun troncamento dei chunk)");

        var logger = new CapturingLogger();
        var body = new string('a', 5 * 1024);
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);
        var ctx = CreateContext("/api", "");
        ctx.Request.Body = new ChunkedStream(Encoding.UTF8.GetBytes(body), 100, throwOnSyncRead: false);

        await middleware.InvokeAsync(ctx);

        _output.WriteLine($"[Restituito] LunghezzaLoggata={(logger.Single().Values["RequestBody"] as string)?.Length} su {body.Length} attesi");
        Assert.Equal(body, logger.Single().Values["RequestBody"]);
    }

    [Fact]
    public async Task InvokeAsync_ResponseBodyOver64KB_LogsTruncatedWithLength() {
        _output.WriteLine("[Scenario] Il middleware successivo scrive una response da 64 KB + 1 byte (65.537 byte)");
        _output.WriteLine("[Atteso] ResponseBody loggato come \"(truncated, 65537 bytes)\" ma la risposta completa raggiunge lo stream originale (65.537 byte)");

        var logger = new CapturingLogger();
        var payload = new byte[64 * 1024 + 1];
        Array.Fill(payload, (byte)'z');
        var middleware = new RequestResponseLoggingMiddleware(ctx => ctx.Response.Body.WriteAsync(payload).AsTask(), logger);
        var ctx = CreateContext("/api", "");
        var original = (MemoryStream)ctx.Response.Body;

        await middleware.InvokeAsync(ctx);

        _output.WriteLine($"[Restituito] ResponseBody=\"{logger.Single().Values["ResponseBody"]}\", ByteNelloStreamOriginale={original.Length}");
        Assert.Equal("(truncated, 65537 bytes)", logger.Single().Values["ResponseBody"]);
        Assert.Equal(payload.Length, original.Length);
    }

    [Fact]
    public async Task InvokeAsync_NonSeekableRequestBodyOver64KB_LogsTruncatedMarker() {
        _output.WriteLine("[Scenario] Request body non-seekable da 70 KB, letto a chunk senza letture sincrone");
        _output.WriteLine("[Atteso] RequestBody loggato termina con \"... (truncated)\"");

        var logger = new CapturingLogger();
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);
        var ctx = CreateContext("/api", "");
        ctx.Request.Body = new ChunkedStream(new byte[70 * 1024].Select(_ => (byte)'q').ToArray(), int.MaxValue, throwOnSyncRead: true);

        try {
            await middleware.InvokeAsync(ctx);

            var logged = Assert.IsType<string>(logger.Single().Values["RequestBody"]);
            _output.WriteLine($"[Restituito] RequestBody termina con marcatore={logged.EndsWith("... (truncated)")}, Lunghezza={logged.Length}");
            Assert.EndsWith("... (truncated)", logged);
        } finally {
            await ctx.Request.Body.DisposeAsync();
        }
    }

    /// <summary>Non-seekable read-only stream; optionally limits bytes per read and rejects synchronous reads.</summary>
    private sealed class ChunkedStream(byte[] data, int maxChunk, bool throwOnSyncRead) : Stream {
        private int _pos;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) {
            if (throwOnSyncRead) throw new InvalidOperationException("Synchronous reads are disallowed.");
            return ReadCore(buffer.AsSpan(offset, count));
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => new(ReadCore(buffer.Span));
        private int ReadCore(Span<byte> dest) {
            var n = Math.Min(Math.Min(dest.Length, maxChunk), data.Length - _pos);
            data.AsSpan(_pos, n).CopyTo(dest);
            _pos += n;
            return n;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed record Entry(LogLevel Level, Exception? Exception, Dictionary<string, object?> Values);

    private sealed class CapturingLogger : ILogger<RequestResponseLoggingMiddleware> {
        public List<Entry> Entries { get; } = [];
        public Entry Single() => Assert.Single(Entries);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) {
            var values = new Dictionary<string, object?>();
            if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
                foreach (var kv in kvps) values[kv.Key] = kv.Value;
            Entries.Add(new Entry(logLevel, exception, values));
        }
    }
}
