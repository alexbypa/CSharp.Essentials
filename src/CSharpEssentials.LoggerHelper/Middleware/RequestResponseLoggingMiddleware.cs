using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Text;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// ASP.NET Core middleware that logs HTTP request and response details.
/// Uses ILogger&lt;T&gt; so logs flow through the LoggerHelper routing pipeline.
/// </summary>
public sealed class RequestResponseLoggingMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    /// <summary>
    /// Maximum body size to capture for logging. For seekable streams with a known length it is a byte
    /// guard (larger bodies are skipped); otherwise it is a cap on the number of characters read.
    /// Larger bodies are truncated to prevent memory exhaustion.
    /// </summary>
    private const int MaxBodySize = 64 * 1024; // 64 KB

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        // SSE and WebSocket endpoints produce long-lived streaming responses
        // that cannot be buffered — skip response body capture entirely.
        var accept = context.Request.Headers.Accept.ToString();
        if (accept.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase)) {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        var faulted = false;

        try {
            context.Request.EnableBuffering();

            var requestBody = await ReadBodySafe(context.Request.Body);
            context.Request.Body.Position = 0;

            await _next(context);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await ReadBodySafe(responseBodyStream);
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Error : LogLevel.Information;

            _logger.Log(logLevel,
                "HTTP {Method} {Path}{QueryString} — Status {StatusCode}\nRequest: {RequestBody}\nResponse: {ResponseBody}",
                SanitizeLogValue(context.Request.Method),
                SanitizeLogValue(context.Request.Path.Value),
                SanitizeLogValue(Uri.UnescapeDataString(context.Request.QueryString.ToString())),
                context.Response.StatusCode,
                string.IsNullOrWhiteSpace(requestBody) ? "(empty)" : SanitizeLogValue(requestBody),
                string.IsNullOrWhiteSpace(responseBody) ? "(empty)" : SanitizeLogValue(responseBody));
        } catch (Exception ex) {
            faulted = true;
            _logger.LogError(ex, "Error processing HTTP {Method} {Path}",
                SanitizeLogValue(context.Request.Method), SanitizeLogValue(context.Request.Path.Value));
            throw;
        } finally {
            // Restore the real stream (the buffer is disposed on exit).
            context.Response.Body = originalBodyStream;
            // Downstream threw and the response has not started: discard the partial buffer so the
            // exception handler downstream/upstream can still write its own error response.
            // Otherwise copy the buffered response from the start (best effort when faulted after start).
            if (!faulted || context.Response.HasStarted) {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                try {
                    await responseBodyStream.CopyToAsync(originalBodyStream, context.RequestAborted);
                } catch (Exception) when (faulted) {
                    // Never mask the downstream exception with a copy failure (e.g. client disconnected).
                }
            }
        }
    }

    /// <summary>
    /// Replaces all line terminators (CR, LF, CRLF, NEL U+0085, LS U+2028, PS U+2029, FF)
    /// with a space to prevent log forging (CodeQL cs/log-forging).
    /// </summary>
    private static string? SanitizeLogValue(string? value) => value?.ReplaceLineEndings(" ");

    /// <summary>
    /// Reads a stream body with size limit to prevent memory exhaustion.
    /// Uses ArrayPool&lt;char&gt; to avoid a 128 KB heap allocation on every request.
    /// </summary>
    private static async Task<string> ReadBodySafe(Stream stream) {
        if (!stream.CanRead)
            return "(unreadable)";

        // For streams with known length, skip if too large
        if (stream.CanSeek && stream.Length > MaxBodySize)
            return $"(truncated, {stream.Length} bytes)";

        using var reader = new StreamReader(
            stream,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        // Rent a shared buffer instead of allocating 128 KB per request on the heap.
        // ArrayPool.Shared returns a thread-local or pooled array — GC-free hot path.
        // One extra char tells "exactly MaxBodySize" apart from "truncated" without a sync Peek().
        var buffer = ArrayPool<char>.Shared.Rent(MaxBodySize + 1);
        try {
            // ReadBlockAsync loops until the buffer is full or the stream ends (ReadAsync may stop at the first chunk).
            var charsRead = await reader.ReadBlockAsync(buffer.AsMemory(0, MaxBodySize + 1));

            if (charsRead == 0)
                return string.Empty;

            if (charsRead > MaxBodySize) {
                // Do not split a surrogate pair at the cut.
                var cut = char.IsHighSurrogate(buffer[MaxBodySize - 1]) ? MaxBodySize - 1 : MaxBodySize;
                return new string(buffer, 0, cut) + "... (truncated)";
            }

            return new string(buffer, 0, charsRead);
        } finally {
            ArrayPool<char>.Shared.Return(buffer, clearArray: true); // bodies may hold tokens/PII
        }
    }
}