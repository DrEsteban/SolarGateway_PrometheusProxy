using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;
using SolarGateway_PrometheusProxy.Models;
using SolarGateway_PrometheusProxy.Support;

namespace SolarGateway_PrometheusProxy.Services;

/// <summary>
/// Provides metrics from a Tesla Gateway and saves them to a Prometheus <see cref="CollectorRegistry"/>.
/// </summary>
public partial class TeslaGatewayMetricsService(
    IHttpClientFactory factory,
    IOptions<TeslaConfiguration> configuration,
    ILogger<TeslaGatewayMetricsService> logger) : MetricsServiceBase(factory.CreateClient(nameof(TeslaGatewayMetricsService)), logger)
{
    private readonly TeslaConfiguration _configuration = configuration.Value;
    private volatile TeslaLoginResponse? _cachedLoginResponse;

    protected override string MetricCategory => "tesla_gateway";

    /// <summary>
    /// Implementation for Tesla Gateway.
    /// <inheritdoc/>
    /// </summary>
    /// <inheritdoc/>
    public override async Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        bool loginCached = true;

        // Ensure we have a valid auth token
        if (string.IsNullOrWhiteSpace(this._cachedLoginResponse?.Token) ||
            (await this.PingTestAsync(cancellationToken)).IsAuthenticationFailure())
        {
            // Get and cache a new auth token
            loginCached = false;
            this._cachedLoginResponse = await this.LoginAsync(cancellationToken);
        }

        // Get metrics in parallel
        var results = await Task.WhenAll(
            this.PullMeterAggregatesAsync(collectorRegistry, cancellationToken),
            this.PullPowerwallPercentageAsync(collectorRegistry, cancellationToken),
            this.PullSiteInfoAsync(collectorRegistry, cancellationToken),
            this.PullStatusAsync(collectorRegistry, cancellationToken),
            this.PullOperationAsync(collectorRegistry, cancellationToken));
        if (!results.All(r => r.IsSuccessStatusCode()))
        {
            if (results.Any(r => r.IsAuthenticationFailure()))
            {
                this._cachedLoginResponse = null;
            }
            int numSuccessful = results.Count(r => r.IsSuccessStatusCode());
            throw new MetricRequestFailedException($"Failed to pull {numSuccessful}/{results.Length} endpoints on Tesla gateway");
        }

        base.SetRequestDurationMetric(collectorRegistry, loginCached, sw.Elapsed);
    }

    /// <summary>
    /// Ensure's the auth token is valid by pinging an authorized endpoint on the gateway.
    /// </summary>
    /// <param name="loginResponse">Optional. If not provided, the <see cref="_cachedLoginResponse"/> will be used.</param>
    private async Task<HttpStatusCode> PingTestAsync(CancellationToken cancellationToken, TeslaLoginResponse? loginResponse = null)
    {
        // Arbitrarily picking /api/operation as a test endpoint
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/operation");
        // Use the provided auth info if available, otherwise use the cached login response
        request.Headers.Authorization = (loginResponse ?? this._cachedLoginResponse)?.AuthenticationHeader;
        using var response = await this._client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return response.StatusCode;
    }

    /// <summary>
    /// Fetches an auth token and ensures it's valid.
    /// </summary>
    /// <exception cref="MetricRequestFailedException">Thrown if we can't get a valid auth token</exception>
    /// <exception cref="Exception">Thrown if we can't deserialize the auth token</exception>
    /// <exception cref="JsonException">Thrown if we can't parse the auth token</exception>
    private async Task<TeslaLoginResponse> LoginAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/login/Basic");
        request.Content = JsonContent.Create<TeslaLoginRequest>(this._configuration.GetTeslaLoginRequest(), JsonModelContext.Default.TeslaLoginRequest);
        using var response = await this._client.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            this._logger.LogDebug("Got {StatusCodeString} ({StatusCode}) calling login endpoint: {Body}",
                (int)response.StatusCode,
                response.StatusCode,
                responseContent);
            throw new MetricRequestFailedException($"Got {(int)response.StatusCode} ({response.StatusCode}) calling login endpoint: {responseContent}");
        }

        var loginResponse = JsonSerializer.Deserialize<TeslaLoginResponse>(responseContent, JsonModelContext.Default.TeslaLoginResponse)
            ?? throw new Exception($"Failed to parse {nameof(TeslaLoginResponse)} for valid token");

        // Ensure token is valid
        if (!(await this.PingTestAsync(cancellationToken, loginResponse)).IsSuccessStatusCode())
        {
            throw new MetricRequestFailedException($"Failed to authenticate and ping the Tesla Gateway after login");
        }
        return loginResponse;
    }

    private async Task<HttpStatusCode> PullMeterAggregatesAsync(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        (var metricsDocument, var statusCode) = await base.CallMetricEndpointAsync("/api/meters/aggregates", this._cachedLoginResponse?.AuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("API Meter aggregates document is null");
            return statusCode;
        }
        using var _ = metricsDocument;

        foreach (var category in metricsDocument.RootElement.EnumerateObject())
        {
            foreach (var metric in category.Value.EnumerateObject())
            {
                switch (metric.Value.ValueKind)
                {
                    case JsonValueKind.Number:
                        base.CreateGauge(registry, category.Name, metric.Name).Set(metric.Value.GetDouble());
                        break;
                    case JsonValueKind.String:
                        // Assumed to be DateTime
                        if (DateTimeOffset.TryParse(metric.Value.GetString(), out var date))
                        {
                            base.CreateGauge(registry, category.Name, metric.Name).Set(date.ToUnixTimeSeconds());
                        }

                        break;
                    default:
                        this._logger.LogWarning("Unsupported ValueKind: {ValueKind}", metric.Value.ValueKind);
                        break;
                }
            }
        }

        return statusCode;
    }

    private async Task<HttpStatusCode> PullPowerwallPercentageAsync(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        (var metricsDocument, var statusCode) = await base.CallMetricEndpointAsync("/api/system_status/soe", this._cachedLoginResponse?.AuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("API Powerwall percentage document is null");
            return statusCode;
        }
        using var _ = metricsDocument;

        base.CreateGauge(registry, "powerwall", "percentage").Set(metricsDocument.RootElement.GetProperty("percentage").GetDouble());
        return statusCode;
    }

    private async Task<HttpStatusCode> PullSiteInfoAsync(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        (var metricsDocument, var statusCode) = await base.CallMetricEndpointAsync("/api/site_info", this._cachedLoginResponse?.AuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("Site info document is null");
            return statusCode;
        }
        using var _ = metricsDocument;

        foreach (var metric in metricsDocument.RootElement.EnumerateObject()
                     .Where(p => p.Value.ValueKind == JsonValueKind.Number))
        {
            base.CreateGauge(registry, "siteinfo", metric.Name).Set(metric.Value.GetDouble());
        }

        return statusCode;
    }

    [GeneratedRegex(@"^(?<hours>[0-9]*)h(?<minutes>[0-9]*)m(?<seconds>[0-9]*)(\.[0-9]*s)?$")]
    private static partial Regex UpTimeRegex();

    private async Task<HttpStatusCode> PullStatusAsync(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        (var metricsDocument, var statusCode) = await base.CallMetricEndpointAsync("/api/status", this._cachedLoginResponse?.AuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("API Status document is null");
            return statusCode;
        }
        using var _ = metricsDocument;

        if (DateTimeOffset.TryParse(metricsDocument.RootElement.GetProperty("start_time").GetString(), out var startTime))
        {
            base.CreateGauge(registry, "status", "start_time").Set(startTime.ToUnixTimeSeconds());
        }

        var match = UpTimeRegex().Match(metricsDocument.RootElement.GetProperty("up_time_seconds").GetString() ?? string.Empty);
        if (match.Success)
        {
            int hours = int.Parse(match.Groups["hours"].Value);
            int minutes = int.Parse(match.Groups["minutes"].Value);
            int seconds = int.Parse(match.Groups["seconds"].Value);
            var timeSpan = new TimeSpan(hours, minutes, seconds);
            base.CreateGauge(registry, "status", "up_time_seconds").Set(timeSpan.TotalSeconds);
        }

        return statusCode;
    }

    private async Task<HttpStatusCode> PullOperationAsync(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        (var metricsDocument, var statusCode) = await base.CallMetricEndpointAsync("/api/operation", this._cachedLoginResponse?.AuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("API Operation document is null");
            return statusCode;
        }
        using var _ = metricsDocument;

        base.CreateGauge(registry, "operation", "backup_reserve_percent").Set(metricsDocument.RootElement.GetProperty("backup_reserve_percent").GetDouble());

        string? realMode = metricsDocument.RootElement.GetProperty("real_mode").GetString();
        Gauge.Child GetModeGauge(string mode) => base.CreateGauge(registry, "operation", "mode", KeyValuePair.Create("mode", mode));

        const string selfConsumption = "self_consumption", autonomous = "autonomous", backup = "backup";
        GetModeGauge(selfConsumption).Set(realMode == selfConsumption ? 1 : 0);
        GetModeGauge(autonomous).Set(realMode == autonomous ? 1 : 0);
        GetModeGauge(backup).Set(realMode == backup ? 1 : 0);

        return statusCode;
    }
}
