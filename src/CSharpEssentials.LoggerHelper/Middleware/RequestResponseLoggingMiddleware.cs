using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Web;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// ASP.NET Core middleware that logs HTTP request and response details.
/// Uses ILogger&lt;T&gt; so logs flow through the LoggerHelper routing pipeline.
/// </summary>
public sealed class RequestResponseLoggingMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    /// <summary>
    /// Maximum body size (in bytes) to capture for logging.
    /// Bodies larger than this are truncated to prevent memory exhaustion.
    /// </summary>
    private const int MaxBodySize = 64 * 1024; // 64 KB

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

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
                context.Request.Method,
                context.Request.Path.Value,
                HttpUtility.UrlDecode(context.Request.QueryString.ToString()),
                context.Response.StatusCode,
                string.IsNullOrWhiteSpace(requestBody) ? "(empty)" : requestBody,
                string.IsNullOrWhiteSpace(responseBody) ? "(empty)" : responseBody);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error processing HTTP {Method} {Path}",
                context.Request.Method, context.Request.Path.Value);
            throw;
        } finally {
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
    }

    /// <summary>
    /// Reads a stream body with size limit to prevent memory exhaustion.
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

        var buffer = new char[MaxBodySize];
        var charsRead = await reader.ReadAsync(buffer, 0, MaxBodySize);

        if (charsRead == 0)
            return string.Empty;

        var result = new string(buffer, 0, charsRead);

        // Check if there's more data (truncated)
        if (charsRead == MaxBodySize && reader.Peek() >= 0)
            return result + "... (truncated)";

        return result;
    }
}
