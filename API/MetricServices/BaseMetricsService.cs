using System.Net.Http.Headers;
using System.Text.Json;
using Prometheus;

namespace SolarGateway_PrometheusProxy.MetricServices;

public abstract class BaseMetricsService : IMetricsService
{
    protected readonly HttpClient _client;
    protected readonly ILogger _logger;

    public BaseMetricsService(HttpClient client, ILogger logger)
        => (_client, _logger) = (client, logger);

    protected abstract string MetricCategory { get; }

    public abstract Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default);

    protected Gauge CreateGauge(CollectorRegistry registry, string subCategory, string metric, params string[] labelNames)
        => Metrics.WithCustomRegistry(registry).CreateGauge($"solarapiproxy_{MetricCategory}_{subCategory}_{metric}", metric, labelNames);

    protected void SetRequestDurationMetric(CollectorRegistry registry, bool loginCached, TimeSpan duration)
    {
        // Request duration metric
        var requestDurationGauge = CreateGauge(registry, "request", "duration_ms", "loginCached");
        var requestDurationGauge_cache = requestDurationGauge.WithLabels("true");
        var requestDurationGauge_nocache = requestDurationGauge.WithLabels("false");
        if (loginCached)
        {
            requestDurationGauge_cache.Set(duration.TotalMilliseconds);
            requestDurationGauge_nocache.Unpublish();
        }
        else
        {
            requestDurationGauge_nocache.Set(duration.TotalMilliseconds);
            requestDurationGauge_cache.Unpublish();
        }
    }

    protected async Task<JsonDocument?> CallMetricEndpointAsync(string path, Func<AuthenticationHeaderValue?> authenticationCallback, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        var authHeader = authenticationCallback();
        if (authHeader != null)
        {
            request.Headers.Authorization = authHeader;
        }

        using var response = await _client.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Got {response.StatusCode} calling '{path}': {responseContent}");
            return null;
        }

        return JsonDocument.Parse(responseContent);
    }
}