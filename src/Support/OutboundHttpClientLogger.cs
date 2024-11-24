using Microsoft.Extensions.Http.Logging;

namespace SolarGateway_PrometheusProxy.Support;

public class OutboundHttpClientLogger(ILogger<OutboundHttpClientLogger> _logger) : IHttpClientLogger
{
    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        (string? host, string? path) = GetRequestInfo(request);
        string elapsedMsString = GetElapsedMsString(elapsed);

        if (exception is OperationCanceledException)
        {
            _logger.LogWarning(
                "Request '{Request.Host}{Request.Path}' was canceled after {Response.ElapsedMilliseconds}ms",
                host,
                path,
                elapsedMsString);
        }
        else
        {
            _logger.LogError(
                exception,
                "Request '{Request.Method} {Request.Host}{Request.Path}' failed after {Response.ElapsedMilliseconds}ms w/ status '{Response.StatusCodeInt}'",
                request.Method,
                host,
                path,
                elapsedMsString,
                (int?)response?.StatusCode);
        }
    }

    public object? LogRequestStart(HttpRequestMessage request)
    {
        (string? host, string? path) = GetRequestInfo(request);
        _logger.LogTrace(
            "Sending '{Request.Method}' to '{Request.Host}{Request.Path}'",
            request.Method,
            host,
            path);
        return null;
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        (string? host, string? path) = GetRequestInfo(request);
        string elapsedMsString = GetElapsedMsString(elapsed);
        var severity = response.IsSuccessStatusCode ? LogLevel.Debug : LogLevel.Information;
        _logger.Log(
            severity,
            "Received '{Response.StatusCodeInt} {Response.StatusCodeString}' " +
            "for request '{Request.Method} {Request.Host}{Request.Path}' " +
            "after {Response.ElapsedMilliseconds}ms",
            (int)response.StatusCode,
            response.StatusCode,
            request.Method,
            host,
            path,
            elapsedMsString);
    }

    private static (string? host, string? path) GetRequestInfo(HttpRequestMessage request)
        => (request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri?.PathAndQuery);

    private static string GetElapsedMsString(TimeSpan elapsed)
        => elapsed.TotalMilliseconds.ToString("F1");
}