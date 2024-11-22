using System.Net.Http.Headers;
using System.Text.Json;
using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;
using SolarGateway_PrometheusProxy.Models;

namespace SolarGateway_PrometheusProxy.Services;

/// <summary>
/// Base class that provides common functionality for brands that use a REST API to collect metrics.
/// </summary>
public abstract class MetricsServiceBase(HttpClient client, ILogger logger) : IMetricsService
{
    protected readonly HttpClient _client = client;
    protected readonly ILogger _logger = logger;

    protected abstract string MetricCategory { get; }

    public abstract Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default);

    protected Gauge.Child CreateGauge(CollectorRegistry registry, string subCategory, string metric, params KeyValuePair<string, string>[] labels)
    {
        var labelKeys = labels.Select(l => l.Key).Concat([$"{this.GetType().Name}Host"]).ToArray();
        var labelValues = labels.Select(l => l.Value).Concat([this._client.BaseAddress!.Host]).ToArray();
        return Metrics.WithCustomRegistry(registry).CreateGauge($"solarapiproxy_{MetricCategory}_{subCategory}_{metric}", metric, labelKeys)
                      .WithLabels(labelValues);
    }

    protected void SetRequestDurationMetric(CollectorRegistry registry, bool loginCached, TimeSpan duration)
    {
        // Request duration metric
        var requestDurationGauge_cache = this.CreateGauge(registry, "request", "duration_ms", KeyValuePair.Create("loginCached", "true"));
        var requestDurationGauge_nocache = this.CreateGauge(registry, "request", "duration_ms", KeyValuePair.Create("loginCached", "false"));
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
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            var authHeader = authenticationCallback();
            if (authHeader != null)
            {
                request.Headers.Authorization = authHeader;
            }

            using var response = await this._client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogError("Got {StatusCode} calling '{Path}': {Body}", 
                    response.StatusCode, 
                    path, 
                    await response.Content.ReadAsStringAsync(CancellationToken.None));
                return null;
            }

            return await response.Content.ReadFromJsonAsync<JsonDocument>(JsonModelContext.Default.JsonDocument, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new MetricRequestFailedException(ex.Message, ex);
        }
    }
}