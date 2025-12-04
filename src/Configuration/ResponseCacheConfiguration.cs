namespace SolarGateway_PrometheusProxy.Configuration;

public class ResponseCacheConfiguration
{
    public int ResponseCacheDurationSeconds { get; set; } = 5;
    public int MetricsRequestTimeoutSeconds { get; set; } = 5;
}
