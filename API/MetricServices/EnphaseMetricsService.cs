using System.Diagnostics;
using System.Text.Json;
using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;

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

        var metricsDocument = await CallMetricEndpointAsync("/production.json", () => null, cancellationToken);
        if (metricsDocument == null)
        {
            throw new MetricRequestFailedException($"Failed to pull metric endpoint endpoints on Enphase gateway");
        }

        var production = metricsDocument.RootElement
            .GetProperty("production")
            .EnumerateArray()
            .First(x => x.GetProperty("type").GetString() == "eim");
        SetMetric(collectorRegistry, nameof(production), "wNow", production);
        SetMetric(collectorRegistry, nameof(production), "whLifetime", production);
        SetMetric(collectorRegistry, nameof(production), "whToday", production);
        SetMetric(collectorRegistry, nameof(production), "whLastSevenDays", production);

        var consumption = metricsDocument.RootElement
            .GetProperty("consumption")
            .EnumerateArray()
            .First(x => x.GetProperty("type").GetString() == "eim" &&
                        x.GetProperty("measurementType").GetString() == "total-consumption");
        SetMetric(collectorRegistry, nameof(consumption), "wNow", consumption);
        SetMetric(collectorRegistry, nameof(consumption), "whLifetime", consumption);
        SetMetric(collectorRegistry, nameof(consumption), "whToday", consumption);
        SetMetric(collectorRegistry, nameof(consumption), "whLastSevenDays", consumption);

        var storage = metricsDocument.RootElement
            .GetProperty("storage")
            .EnumerateArray()
            .First(x => x.GetProperty("type").GetString() == "acb");
        SetMetric(collectorRegistry, nameof(storage), "wNow", storage);
        SetMetric(collectorRegistry, nameof(storage), "whNow", storage);

        SetRequestDurationMetric(collectorRegistry, false, sw.Elapsed);
    }

    private void SetMetric(CollectorRegistry collectorRegistry, string type, string metric, JsonElement element)
        => CreateGauge(collectorRegistry, type, metric).Set(element.GetProperty(metric).GetDouble());
}