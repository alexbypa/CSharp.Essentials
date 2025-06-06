﻿using Microsoft.AspNetCore.Http;
using Serilog.Events;
using System.Text;
using System.Web;

namespace CSharpEssentials.LoggerHelper;

public class RequestInfo : IRequest {
    public string IdTransaction { get; set; } = Guid.NewGuid().ToString();
    public string Action { get; set; }
    public string ApplicationName { get; set; }
}
public class RequestResponseLoggingMiddleware {
    private readonly RequestDelegate _next;
    public RequestResponseLoggingMiddleware(RequestDelegate next) {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context) {
        var requestInfo = new RequestInfo {
            Action = $"{context.Request.Method}_{context.Request.Path}"
        };
        // Prepara per catturare la risposta
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

            string body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            await _next(context);

            var logLevel = context.Response.StatusCode >= 400
                ? LogEventLevel.Error
                : LogEventLevel.Information;

            var responseBody = await ReadResponseBody(context.Response);
            var formattedLog = @$"
HTTP METHOD : {context.Request.Method}
PATH         : {context.Request.Path}
QUERY STRING : {HttpUtility.UrlDecode(context.Request.QueryString.ToString())}
BODY REQUEST : {(string.IsNullOrWhiteSpace(body) ? "(empty)" : body)}
BODY RESPONSE: {(string.IsNullOrWhiteSpace(responseBody) ? "(empty)" : responseBody)}
HTTP STATUS  : {(context.Response.StatusCode)}
";
            loggerExtension<RequestInfo>.TraceAsync(
                requestInfo,
                logLevel,
                null,
                formattedLog
            );
        } catch (Exception ex) {
            // Log dell'eccezione
            loggerExtension<RequestInfo>.TraceAsync(
                requestInfo,
                LogEventLevel.Error,
                ex,
                "ERROR PROCESSING REQUEST"
            );
            throw;
        } finally {
            // Copia la risposta nel body originale
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> ReadRequestBody(HttpRequest request) {
        request.EnableBuffering();

        using var reader = new StreamReader(
            request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private async Task<string> ReadResponseBody(HttpResponse response) {
        response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return text;
    }

}