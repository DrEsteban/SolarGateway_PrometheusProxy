namespace SolarGateway_PrometheusProxy.Exceptions;

public class MetricRequestFailedException : Exception
{
    public MetricRequestFailedException(string message) : base(message)
    { }
}