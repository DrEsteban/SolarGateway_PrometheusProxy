namespace SolarGateway_PrometheusProxy.Services;

public interface IMetricsCollectionService
{
    /// <summary>
    /// Collects metrics from all registered services, ensuring only one collection happens at a time
    /// and results are cached according to configuration.
    /// </summary>
    Task CollectAllAsync(CancellationToken cancellationToken = default);
}
