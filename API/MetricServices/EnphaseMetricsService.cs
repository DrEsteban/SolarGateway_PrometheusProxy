using System.Diagnostics;
using Prometheus;

namespace SolarGateway_PrometheusProxy.MetricServices;

public class EnphaseMetricsService : BaseMetricsService
{
    public EnphaseMetricsService(
        IHttpClientFactory httpClientFactory,
        ILogger<EnphaseMetricsService> logger)
        : base(httpClientFactory.CreateClient(nameof(EnphaseMetricsService)), logger)
    { }

    protected override string MetricCategory => "enphase";

    public override async Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var metricsDocument = await CallMetricEndpointAsync("/production.json", () => null);
        // TODO get metrics

        SetRequestDurationMetric(collectorRegistry, false, sw.Elapsed);
    }
}