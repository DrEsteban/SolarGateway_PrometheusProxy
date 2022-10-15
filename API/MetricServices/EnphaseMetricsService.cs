
using Prometheus;

namespace TeslaGateway_PrometheusProxy;

public class EnphraseMetricsService : IMetricsService
{
    public Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}