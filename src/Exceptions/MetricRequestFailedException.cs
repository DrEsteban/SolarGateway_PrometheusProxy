namespace SolarGateway_PrometheusProxy.Exceptions;

public class MetricRequestFailedException : Exception
{
    public MetricRequestFailedException(string message) : base(message)
    { }

    public MetricRequestFailedException(string message, Exception innerException) : base(message, innerException)
    { }
}