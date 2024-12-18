using System.Net;
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

    /// <inheritdoc/>
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

    /// <summary>
    /// Calls a metric endpoint and returns the JSON document and status code.
    /// </summary>
    /// <param name="httpMethod">Defaults to GET if unspecified</param>
    /// <exception cref="MetricRequestFailedException"></exception>
    protected async Task<(JsonDocument? Document, HttpStatusCode StatusCode)> CallMetricEndpointAsync(
        string path,
        AuthenticationHeaderValue? authenticationHeader,
        CancellationToken cancellationToken,
        HttpMethod? httpMethod = null)
    {
        httpMethod ??= HttpMethod.Get;
        try
        {
            using var request = new HttpRequestMessage(httpMethod, path);
            if (authenticationHeader != null)
            {
                request.Headers.Authorization = authenticationHeader;
            }

            using var response = await this._client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (this._logger.IsEnabled(LogLevel.Debug))
                {
                    this._logger.LogDebug("Got {StatusCode} calling '{Path}': {Body}",
                        response.StatusCode,
                        path,
                        await response.Content.ReadAsStringAsync(CancellationToken.None));
                }
                return (null, response.StatusCode);
            }

            var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonModelContext.Default.JsonDocument, cancellationToken);
            return (doc, response.StatusCode);
        }
        catch (Exception ex)
        {
            throw new MetricRequestFailedException(ex.Message, ex);
        }
    }
}