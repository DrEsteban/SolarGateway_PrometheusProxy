using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;
using SolarGateway_PrometheusProxy.Models;

namespace SolarGateway_PrometheusProxy.MetricServices;

public class TeslaGatewayMetricsService : BaseMetricsService
{
    private readonly TeslaLoginRequest _loginRequest;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _loginCacheLength;

    public TeslaGatewayMetricsService(
        IOptions<TeslaLoginRequest> loginRequest,
        ILogger<TeslaGatewayMetricsService> logger,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        IOptions<TeslaConfiguration> configuration)
        : base(httpClientFactory.CreateClient(nameof(TeslaGatewayMetricsService)), logger)
    {
        _loginRequest = loginRequest.Value;
        _cache = cache;
        _loginCacheLength = TimeSpan.FromMinutes(configuration.Value.LoginCacheMinutes);
    }

    protected override string MetricCategory => "tesla_gateway";

    /// <summary>
    /// Collects metrics from the Tesla Gateway saves to the Prometheus <see cref="CollectorRegistry"/>.
    /// </summary>
    /// <exception cref="MetricRequestFailedException">Thrown when the Tesla Gateway returns an unexpected response.</exception>
    /// <exception cref="Exception"></exception>
    public override async Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        bool loginCached = true;

        // Get token
        var loginResponse = await _cache.GetOrCreateAsync("gateway_token", async e =>
        {
            loginCached = false;
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/login/Basic");
            request.Content = JsonContent.Create(_loginRequest);
            using var response = await _client.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Got {response.StatusCode} calling login endpoint: {responseContent}");
                // Prevent bombarding the login endpoint
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                return null;
            }

            e.AbsoluteExpirationRelativeToNow = _loginCacheLength;
            return JsonSerializer.Deserialize<TeslaLoginResponse>(responseContent);
        });

        if (loginResponse is null)
        {
            throw new MetricRequestFailedException("Login response was null");
        }

        if (string.IsNullOrEmpty(loginResponse.Token))
        {
            string err = $"Failed to parse {nameof(TeslaLoginResponse)} for valid token";
            _logger.LogError(err);
            throw new Exception(err);
        }

        // Get metrics
        var results = await Task.WhenAll(
            PullMeterAggregates(collectorRegistry, loginResponse, cancellationToken),
            PullPowerwallPercentage(collectorRegistry, loginResponse, cancellationToken),
            PullSiteInfo(collectorRegistry, loginResponse, cancellationToken),
            PullStatus(collectorRegistry, loginResponse, cancellationToken),
            PullOperation(collectorRegistry, loginResponse, cancellationToken));
        if (!results.All(r => r))
        {
            throw new MetricRequestFailedException($"Failed to pull {results.Count(r => !r)}/{results.Length} endpoints on Tesla gateway");
        }

        SetRequestDurationMetric(collectorRegistry, loginCached, sw.Elapsed);
    }
    
    private async Task<bool> PullMeterAggregates(CollectorRegistry registry, TeslaLoginResponse loginResponse, CancellationToken cancellationToken)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/meters/aggregates", loginResponse.ToAuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            return false;
        }

        foreach (var category in metricsDocument.RootElement.EnumerateObject())
        {
            foreach (var metric in category.Value.EnumerateObject())
            {
                switch (metric.Value.ValueKind)
                {
                    case JsonValueKind.Number:
                        CreateGauge(registry, category.Name, metric.Name).Set(metric.Value.GetDouble());
                        break;
                    case JsonValueKind.String:
                        // Assumed to be DateTime
                        if (DateTimeOffset.TryParse(metric.Value.GetString(), out var date))
                        {
                            CreateGauge(registry, category.Name, metric.Name).Set(date.SecondsSinceEpoch());
                        }

                        break;
                    default:
                        _logger.LogWarning($"Unsupported ValueKind: {metric.Value.ValueKind}");
                        break;
                }
            }
        }

        return true;
    }

    private async Task<bool> PullPowerwallPercentage(CollectorRegistry registry, TeslaLoginResponse loginResponse, CancellationToken cancellationToken)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/system_status/soe", loginResponse.ToAuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            return false;
        }

        CreateGauge(registry, "powerwall", "percentage").Set(metricsDocument.RootElement.GetProperty("percentage").GetDouble());
        return true;
    }

    private async Task<bool> PullSiteInfo(CollectorRegistry registry, TeslaLoginResponse loginResponse, CancellationToken cancellationToken)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/site_info", loginResponse.ToAuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            return false;
        }

        foreach (var metric in metricsDocument.RootElement.EnumerateObject()
                     .Where(p => p.Value.ValueKind == JsonValueKind.Number))
        {
            CreateGauge(registry, "siteinfo", metric.Name).Set(metric.Value.GetDouble());
        }

        return true;
    }

    private static Regex UpTimeRegex = new Regex("^(?<hours>[0-9]*)h(?<minutes>[0-9]*)m(?<seconds>[0-9]*)(\\.[0-9]*s)?$", RegexOptions.Compiled);

    private async Task<bool> PullStatus(CollectorRegistry registry, TeslaLoginResponse loginResponse, CancellationToken cancellationToken)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/status", loginResponse.ToAuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            return false;
        }

        if (DateTimeOffset.TryParse(metricsDocument.RootElement.GetProperty("start_time").GetString(), out var startTime))
        {
            CreateGauge(registry, "status", "start_time").Set(startTime.SecondsSinceEpoch());
        }

        var match = UpTimeRegex.Match(metricsDocument.RootElement.GetProperty("up_time_seconds").GetString() ?? string.Empty);
        if (match.Success)
        {
            int hours = int.Parse(match.Groups["hours"].Value);
            int minutes = int.Parse(match.Groups["minutes"].Value);
            int seconds = int.Parse(match.Groups["seconds"].Value);
            var timeSpan = new TimeSpan(hours, minutes, seconds);
            CreateGauge(registry, "status", "up_time_seconds").Set(timeSpan.TotalSeconds);
        }

        return true;
    }

    private async Task<bool> PullOperation(CollectorRegistry registry, TeslaLoginResponse loginResponse, CancellationToken cancellationToken)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/operation", loginResponse.ToAuthenticationHeader, cancellationToken);
        if (metricsDocument is null)
        {
            return false;
        }

        CreateGauge(registry, "operation", "backup_reserve_percent").Set(metricsDocument.RootElement.GetProperty("backup_reserve_percent").GetDouble());
        
        string? mode = metricsDocument.RootElement.GetProperty("real_mode").GetString();
        var modeGauge = CreateGauge(registry, "operation", "mode", "mode");

        const string selfConsumption = "self_consumption", autonomous = "autonomous", backup = "backup";
        modeGauge.WithLabels(selfConsumption).Set(mode == selfConsumption ? 1 : 0);
        modeGauge.WithLabels(autonomous).Set(mode == autonomous ? 1 : 0);
        modeGauge.WithLabels(backup).Set(mode == backup ? 1 : 0);

        return true;
    }
}