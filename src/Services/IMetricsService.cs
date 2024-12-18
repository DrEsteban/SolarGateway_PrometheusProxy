using Prometheus;

namespace SolarGateway_PrometheusProxy.Services;

/// <summary>
/// Implemented by services that collect metrics from a solar brand's device(s) and save them to a Prometheus <see cref="CollectorRegistry"/>.
/// </summary>
public interface IMetricsService
{
    Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default);
}