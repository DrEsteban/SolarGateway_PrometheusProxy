namespace TeslaGateway_PrometheusProxy;

public class MetricRequestFailedException : Exception
{
    public MetricRequestFailedException(string message) : base(message)
    { }
}