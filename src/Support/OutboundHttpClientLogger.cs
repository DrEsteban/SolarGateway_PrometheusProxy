using Microsoft.Extensions.Http.Logging;

namespace SolarGateway_PrometheusProxy.Support;

public class OutboundHttpClientLogger(ILogger<OutboundHttpClientLogger> _logger) : IHttpClientLogger
{
    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        _logger.LogError(
            exception,
            "Request '{Request.Host}{Request.Path}' failed after {Response.ElapsedMilliseconds}ms",
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri!.PathAndQuery,
            elapsed.TotalMilliseconds.ToString("F1"));
    }

    public object? LogRequestStart(HttpRequestMessage request)
    {
        _logger.LogInformation(
            "Sending '{Request.Method}' to '{Request.Host}{Request.Path}'",
            request.Method,
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri!.PathAndQuery);
        return null;
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        _logger.LogInformation(
            "Received '{Response.StatusCodeInt} {Response.StatusCodeString}' after {Response.ElapsedMilliseconds}ms",
            (int)response.StatusCode,
            response.StatusCode,
            elapsed.TotalMilliseconds.ToString("F1"));
    }
}
