using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;

namespace SolarGateway_PrometheusProxy.Services;

/// <summary>
/// Provides metrics from an Enphase gateway and saves them to the Prometheus <see cref="CollectorRegistry"/>.
/// </summary>
/// <remarks>
/// This class was built by doing limited reverse engineering of a friend's Enphase gateway.
/// There may be more metrics available with additional investigation.
/// </remarks>
public class EnphaseMetricsService(
    HttpClient httpClient,
    ILogger<EnphaseMetricsService> logger,
    ILoggerFactory loggerFactory) : MetricsServiceBase(httpClient, logger, loggerFactory)
{
    protected override string MetricCategory => "enphase";

    protected override Task<AuthenticationHeaderValue?> FetchAuthenticationHeaderAsync(CancellationToken cancellationToken)
        => Task.FromResult<AuthenticationHeaderValue?>(null);

    public override async Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        using var metricsDocument = (await base.CallMetricEndpointAsync("/production.json", cancellationToken))
            ?? throw new MetricRequestFailedException($"Failed to pull metric endpoint endpoints on Enphase gateway");
        var production = metricsDocument.RootElement
            .GetProperty("production")
            .EnumerateArray()
            .First(x => x.GetProperty("type").GetString() == "eim");
        this.SetMetric(collectorRegistry, nameof(production), "wNow", production);
        this.SetMetric(collectorRegistry, nameof(production), "whLifetime", production);
        this.SetMetric(collectorRegistry, nameof(production), "whToday", production);
        this.SetMetric(collectorRegistry, nameof(production), "whLastSevenDays", production);

        var consumption = metricsDocument.RootElement
            .GetProperty("consumption")
            .EnumerateArray()
            .First(x => x.GetProperty("type").GetString() == "eim" &&
                        x.GetProperty("measurementType").GetString() == "total-consumption");
        this.SetMetric(collectorRegistry, nameof(consumption), "wNow", consumption);
        this.SetMetric(collectorRegistry, nameof(consumption), "whLifetime", consumption);
        this.SetMetric(collectorRegistry, nameof(consumption), "whToday", consumption);
        this.SetMetric(collectorRegistry, nameof(consumption), "whLastSevenDays", consumption);

        var storage = metricsDocument.RootElement
            .GetProperty("storage")
            .EnumerateArray()
            .First(x => x.GetProperty("type").GetString() == "acb");
        this.SetMetric(collectorRegistry, nameof(storage), "wNow", storage);
        this.SetMetric(collectorRegistry, nameof(storage), "whNow", storage);

        base.SetRequestDurationMetric(collectorRegistry, sw.Elapsed);
    }

    private void SetMetric(CollectorRegistry collectorRegistry, string type, string metric, JsonElement element)
        => base.CreateGauge(collectorRegistry, type, metric).Set(element.GetProperty(metric).GetDouble());
}