namespace SolarGateway_PrometheusProxy.Exceptions;

public class MetricRequestFailedException : Exception
{
    public int StatusCode { get; }

    public MetricRequestFailedException(string message, int statusCode = 503) : base(message)
    {
        StatusCode = statusCode;
    }

    public MetricRequestFailedException(string message, Exception innerException, int statusCode = 503) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}