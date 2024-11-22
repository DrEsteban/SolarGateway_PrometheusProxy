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
    /// <summary>
    /// The HTTP client used to call the brand's API.
    /// </summary>
    protected readonly HttpClient _client = client;

    /// <summary>
    /// The logger used to log messages.
    /// </summary>
    protected readonly ILogger _logger = logger;

    private readonly SemaphoreSlim _authTokenRefreshLock = new(1, 1);
    private bool _authTokenRefreshed;
    private AuthenticationHeaderValue? _authenticationHeader;

    /// <summary>
    /// Main method to collect metrics from the brand's device(s) and save them to the Prometheus <see cref="CollectorRegistry"/>.
    /// </summary>
    public abstract Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default);

    /// <summary>
    /// The category of metrics this service provides. Used to name metrics in Prometheus.
    /// </summary>
    protected abstract string MetricCategory { get; }

    /// <summary>
    /// Whether this service requires an authentication token to fetch metrics.
    /// </summary>
    protected abstract bool UsesAuthToken { get; }

    /// <summary>
    /// Fetches an authentication header to use when calling the brand's API.
    /// </summary>
    /// <remarks>
    /// If <see cref="UsesAuthToken"/> is <see langword="false">, this method will not be called."/>
    /// </remarks>
    protected abstract Task<AuthenticationHeaderValue?> FetchAuthenticationHeaderAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a Prometheus gauge metric with the given subcategory and metric name.
    /// </summary>
    protected Gauge.Child CreateGauge(CollectorRegistry registry, string subCategory, string metric, params KeyValuePair<string, string>[] labels)
    {
        var labelKeys = labels.Select(l => l.Key).Concat([$"{this.GetType().Name}Host"]).ToArray();
        var labelValues = labels.Select(l => l.Value).Concat([this._client.BaseAddress!.Host]).ToArray();
        return Metrics.WithCustomRegistry(registry)
                      .CreateGauge($"solarapiproxy_{MetricCategory}_{subCategory}_{metric}", metric, labelKeys)
                      .WithLabels(labelValues);
    }

    /// <summary>
    /// Sets the request duration metric for this service.
    /// </summary>
    /// <remarks>
    /// <see cref="CollectMetricsAsync(CollectorRegistry, CancellationToken)"/> should call this method at the end of its execution.
    /// </remarks>
    protected void SetRequestDurationMetric(CollectorRegistry registry, TimeSpan duration)
    {
        // Request duration metric
        var requestDurationGauge_loginCached = this.CreateGauge(registry, "request", "duration_ms", KeyValuePair.Create("loginCached", "true"));
        var requestDurationGauge_loginNotCached = this.CreateGauge(registry, "request", "duration_ms", KeyValuePair.Create("loginCached", "false"));
        if (this._authTokenRefreshed)
        {
            requestDurationGauge_loginNotCached.Set(duration.TotalMilliseconds);
            requestDurationGauge_loginCached.Unpublish();
        }
        else
        {
            requestDurationGauge_loginCached.Set(duration.TotalMilliseconds);
            requestDurationGauge_loginNotCached.Unpublish();
        }

        this._authTokenRefreshed = false;
    }

    /// <summary>
    /// Calls the metric endpoint and returns the JSON document.
    /// </summary>
    /// <remarks>
    /// Should be called in <see cref="CollectMetricsAsync(CollectorRegistry, CancellationToken)"/> to fetch metrics.
    /// </remarks>
    protected async Task<JsonDocument?> CallMetricEndpointAsync(string path, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        try
        {
            if (this.UsesAuthToken && this._authenticationHeader == null)
            {
                await this.RefreshTokenAsync(cancellationToken);
            }

            async Task<HttpResponseMessage> SendRequestAsync()
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.Authorization = this._authenticationHeader;

                return await this._client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }

            response = await SendRequestAsync();

            if (this.UsesAuthToken && response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                // Try refresh token and issue again
                await this.RefreshTokenAsync(cancellationToken);
                response.Dispose();
                response = await SendRequestAsync();
            }

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
        catch (MetricRequestFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to call '{Path}'", path);
            return null;
        }
        finally
        {
            response?.Dispose();
        }
    }

    private async ValueTask RefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (!this.UsesAuthToken || this._authTokenRefreshed)
        {
            return;
        }

        await _authTokenRefreshLock.WaitAsync(cancellationToken);
        try
        {
            if (this._authTokenRefreshed)
            {
                // Another thread has set the token
                return;
            }
            this._authenticationHeader = await this.FetchAuthenticationHeaderAsync(cancellationToken);
            this._authTokenRefreshed = true;
        }
        finally
        {
            _authTokenRefreshLock.Release();
        }
    }
}