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

    protected Gauge.Child CreateGauge(CollectorRegistry registry, string subCategory, string metric, params KeyValuePair<string, string>[] labels)
    {
        var labelKeys = labels.Select(l => l.Key).Concat(new[] { $"{this.GetType().Name}Host" }).ToArray();
        var labelValues = labels.Select(l => l.Value).Concat(new[] { _client.BaseAddress!.Host }).ToArray();
        return Metrics.WithCustomRegistry(registry).CreateGauge($"solarapiproxy_{MetricCategory}_{subCategory}_{metric}", metric, labelKeys)
                      .WithLabels(labelValues);
    }

    protected void SetRequestDurationMetric(CollectorRegistry registry, bool loginCached, TimeSpan duration)
    {
        // Request duration metric
        var requestDurationGauge_cache = CreateGauge(registry, "request", "duration_ms", new KeyValuePair<string, string>("loginCached", "true"));
        var requestDurationGauge_nocache = CreateGauge(registry, "request", "duration_ms", new KeyValuePair<string, string>("loginCached", "false"));
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