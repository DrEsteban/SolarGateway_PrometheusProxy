using Prometheus;

namespace SolarGateway_PrometheusProxy.MetricServices;

public class EnphaseMetricsService : BaseMetricsService
{
    public EnphaseMetricsService(
        IHttpClientFactory httpClientFactory,
        ILogger<EnphaseMetricsService> logger)
        : base(httpClientFactory.CreateClient(nameof(EnphaseMetricsService)), logger)
    { }

    public override async Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}