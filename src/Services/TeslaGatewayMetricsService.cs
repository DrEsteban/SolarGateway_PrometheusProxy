using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;
using SolarGateway_PrometheusProxy.Models;

namespace SolarGateway_PrometheusProxy.Services;

/// <summary>
/// Provides metrics from a Tesla Gateway and saves them to a Prometheus <see cref="CollectorRegistry"/>.
/// </summary>
public partial class TeslaGatewayMetricsService(
    HttpClient httpClient,
    IOptions<TeslaConfiguration> configuration,
    IOptions<TeslaLoginRequest> loginRequest,
    ILogger<TeslaGatewayMetricsService> logger) : MetricsServiceBase(httpClient, logger)
{
    private readonly TeslaLoginRequest _loginRequest = loginRequest.Value;
    private readonly TimeSpan _loginCacheLength = TimeSpan.FromMinutes(configuration.Value.LoginCacheMinutes);

    protected override string MetricCategory => "tesla_gateway";

    protected override bool UsesAuthToken => true;

    protected override async Task<AuthenticationHeaderValue?> FetchAuthenticationHeaderAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/login/Basic");
        request.Content = JsonContent.Create<TeslaLoginRequest>(this._loginRequest, JsonModelContext.Default.TeslaLoginRequest);
        using var response = await this._client.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            this._logger.LogError("Got {StatusCodeString} ({StatusCode}) calling login endpoint: {Body}",
                (int)response.StatusCode,
                response.StatusCode,
                responseContent);
            throw new MetricRequestFailedException($"Got {(int)response.StatusCode} ({response.StatusCode}) calling login endpoint: {responseContent}");
        }

        var loginResponse = JsonSerializer.Deserialize<TeslaLoginResponse>(responseContent, JsonModelContext.Default.TeslaLoginResponse);
        if (string.IsNullOrEmpty(loginResponse?.Token))
        {
            const string err = $"Failed to parse {nameof(TeslaLoginResponse)} for valid token";
            this._logger.LogError(err);
            throw new Exception(err);
        }
        this._logger.LogDebug("Fetched new token: {Token}", JsonSerializer.Serialize<TeslaLoginResponse>(loginResponse, JsonModelContext.Default.TeslaLoginResponse));
        return loginResponse.ToAuthenticationHeader();
    }

    /// <summary>
    /// Collects metrics from the Tesla Gateway saves to the Prometheus <see cref="CollectorRegistry"/>.
    /// </summary>
    /// <exception cref="MetricRequestFailedException">Thrown when the Tesla Gateway returns an unexpected response.</exception>
    /// <exception cref="Exception"></exception>
    public override async Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // Get metrics in parallel
        var results = await Task.WhenAll(
            this.PullMeterAggregates(collectorRegistry, cancellationToken),
            this.PullPowerwallPercentage(collectorRegistry, cancellationToken),
            this.PullSiteInfo(collectorRegistry, cancellationToken),
            this.PullStatus(collectorRegistry, cancellationToken),
            this.PullOperation(collectorRegistry, cancellationToken));

        base.SetRequestDurationMetric(collectorRegistry, sw.Elapsed);
        if (!results.All(r => r))
        {
            throw new MetricRequestFailedException($"Failed to pull {results.Count(r => !r)}/{results.Length} endpoints on Tesla gateway");
        }
    }

    private async Task<bool> PullMeterAggregates(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        using var metricsDocument = await base.CallMetricEndpointAsync("/api/meters/aggregates", cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("API Meter aggregates document is null");
            return false;
        }

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

        return true;
    }

    private async Task<bool> PullPowerwallPercentage(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        using var metricsDocument = await base.CallMetricEndpointAsync("/api/system_status/soe", cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("API Powerwall percentage document is null");
            return false;
        }

        base.CreateGauge(registry, "powerwall", "percentage").Set(metricsDocument.RootElement.GetProperty("percentage").GetDouble());
        return true;
    }

    private async Task<bool> PullSiteInfo(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        using var metricsDocument = await base.CallMetricEndpointAsync("/api/site_info", cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("Site info document is null");
            return false;
        }

        foreach (var metric in metricsDocument.RootElement.EnumerateObject()
                     .Where(p => p.Value.ValueKind == JsonValueKind.Number))
        {
            base.CreateGauge(registry, "siteinfo", metric.Name).Set(metric.Value.GetDouble());
        }

        return true;
    }

    [GeneratedRegex(@"^(?<hours>[0-9]*)h(?<minutes>[0-9]*)m(?<seconds>[0-9]*)(\.[0-9]*s)?$")]
    private static partial Regex UpTimeRegex();

    private async Task<bool> PullStatus(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        using var metricsDocument = await base.CallMetricEndpointAsync("/api/status", cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("API Status document is null");
            return false;
        }

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

        return true;
    }

    private async Task<bool> PullOperation(CollectorRegistry registry, CancellationToken cancellationToken)
    {
        using var metricsDocument = await base.CallMetricEndpointAsync("/api/operation", cancellationToken);
        if (metricsDocument is null)
        {
            this._logger.LogError("API Operation document is null");
            return false;
        }

        base.CreateGauge(registry, "operation", "backup_reserve_percent").Set(metricsDocument.RootElement.GetProperty("backup_reserve_percent").GetDouble());

        string? realMode = metricsDocument.RootElement.GetProperty("real_mode").GetString();
        Gauge.Child GetModeGauge(string mode) => base.CreateGauge(registry, "operation", "mode", KeyValuePair.Create("mode", mode));

        const string selfConsumption = "self_consumption", autonomous = "autonomous", backup = "backup";
        GetModeGauge(selfConsumption).Set(realMode == selfConsumption ? 1 : 0);
        GetModeGauge(autonomous).Set(realMode == autonomous ? 1 : 0);
        GetModeGauge(backup).Set(realMode == backup ? 1 : 0);

        return true;
    }
}