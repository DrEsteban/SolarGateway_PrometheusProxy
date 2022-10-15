using Prometheus;

namespace SolarGateway_PrometheusProxy;

public interface IMetricsService
{
    Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default);
}