using Prometheus;

namespace TeslaGateway_PrometheusProxy;

public interface IMetricsService
{
    Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default);
}