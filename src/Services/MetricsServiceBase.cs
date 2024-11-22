using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Polly;
using Polly.Retry;
using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;
using SolarGateway_PrometheusProxy.Models;

namespace SolarGateway_PrometheusProxy.Services;

/// <summary>
/// Base class that provides common functionality for brands that use a REST API to collect metrics.
/// </summary>
public abstract class MetricsServiceBase : IMetricsService
{
    protected readonly HttpClient _client;
    protected readonly ILogger _logger;
    protected readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

    public MetricsServiceBase(HttpClient client, ILogger logger, ILoggerFactory factory)
    {
        this._client = client;
        this._logger = logger;
        this._resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .ConfigureTelemetry(factory)
            .AddRetry(
                new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(result => result.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden),
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.Zero,
                    OnRetry = async args =>
                    {
                        this.AuthenticationHeader = await this.FetchAuthenticationHeaderAsync(args.Context.CancellationToken);
                    }
                })
            .Build();
    }

    protected abstract string MetricCategory { get; }

    protected AuthenticationHeaderValue? AuthenticationHeader { get; private set; }

    protected abstract Task<AuthenticationHeaderValue?> FetchAuthenticationHeaderAsync(CancellationToken cancellationToken);

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

    protected async Task<JsonDocument?> CallMetricEndpointAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            this.AuthenticationHeader ??= await this.FetchAuthenticationHeaderAsync(cancellationToken);

            using var response = await _resiliencePipeline.ExecuteAsync(async token =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.Authorization = this.AuthenticationHeader;

                return await this._client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            }, cancellationToken);

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