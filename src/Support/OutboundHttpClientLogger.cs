using Microsoft.Extensions.Http.Logging;

namespace SolarGateway_PrometheusProxy.Support;

public class OutboundHttpClientLogger(ILogger<OutboundHttpClientLogger> _logger) : IHttpClientLogger
{
    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        _logger.LogError(
            exception,
            "Request '{Request.Host}{Request.Path}' failed after {Response.ElapsedMilliseconds}ms w/ status '{Response.StatusCodeInt}'",
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri?.PathAndQuery,
            elapsed.TotalMilliseconds.ToString("F1"),
            (int?)response?.StatusCode);
    }

    public object? LogRequestStart(HttpRequestMessage request)
    {
        _logger.LogTrace(
            "Sending '{Request.Method}' to '{Request.Host}{Request.Path}'",
            request.Method,
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri?.PathAndQuery);
        return null;
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        var severity = response.IsSuccessStatusCode ? LogLevel.Debug : LogLevel.Information;
        _logger.Log(
            severity,
            "Received '{Response.StatusCodeInt} {Response.StatusCodeString}' " +
            "for request '{Request.Method} {Request.Host}{Request.Path}' " +
            "after {Response.ElapsedMilliseconds}ms",
            (int)response.StatusCode,
            response.StatusCode,
            request.Method,
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri?.PathAndQuery,
            elapsed.TotalMilliseconds.ToString("F1"));
    }
}
