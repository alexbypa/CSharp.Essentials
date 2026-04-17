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
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            await _next(context);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
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
}
