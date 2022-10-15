using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Prometheus;
using TeslaGateway_PrometheusProxy.Exceptions;
using TeslaGateway_PrometheusProxy.Models;

namespace TeslaGateway_PrometheusProxy;

public class TeslaGatewayMetricsService : IMetricsService
{
    private readonly LoginRequest _loginRequest;
    private readonly ILogger<TeslaGatewayMetricsService> _logger;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _client;

    public TeslaGatewayMetricsService(
        IOptions<LoginRequest> loginRequest,
        ILogger<TeslaGatewayMetricsService> logger,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory)
    {
        _loginRequest = loginRequest.Value;
        _logger = logger;
        _cache = cache;
        _client = httpClientFactory.CreateClient(nameof(TeslaGatewayMetricsService));
    }

    /// <summary>
    /// Collects metrics from the Tesla Gateway saves to the Prometheus <see cref="CollectorRegistry"/>.
    /// </summary>
    /// <exception cref="MetricRequestFailedException">Thrown when the Tesla Gateway returns an unexpected response.</exception>
    /// <exception cref="Exception"></exception>
    public async Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        bool loginCached = true;

        // Get token
        var loginResponse = await _cache.GetOrCreateAsync("gateway_token", async e =>
        {
            loginCached = false;
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/login/Basic");
            request.Content = JsonContent.Create(_loginRequest);
            using var response = await _client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Got {response.StatusCode} calling login endpoint: {responseContent}");
                // Prevent bombarding the login endpoint
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                return null;
            }

            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return JsonSerializer.Deserialize<LoginResponse>(responseContent);
        });

        if (loginResponse is null)
        {
            throw new MetricRequestFailedException("Login response was null");
        }

        if (string.IsNullOrEmpty(loginResponse.Token))
        {
            string err = $"Failed to parse {nameof(LoginResponse)} for valid token";
            _logger.LogError(err);
            throw new Exception(err);
        }

        // Get metrics
        var results = await Task.WhenAll(
            PullMeterAggregates(collectorRegistry, loginResponse),
            PullPowerwallPercentage(collectorRegistry, loginResponse),
            PullSiteInfo(collectorRegistry, loginResponse),
            PullStatus(collectorRegistry, loginResponse),
            PullOperation(collectorRegistry, loginResponse));
        if (!results.All(r => r))
        {
            throw new MetricRequestFailedException($"Failed to pull {results.Count(r => !r)}/{results.Length} endpoints on gateway");
        }

        SetRequestDurationMetric(collectorRegistry, loginCached, sw.Elapsed);
    }
    
    private async Task<bool> PullMeterAggregates(CollectorRegistry registry, LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/meters/aggregates", loginResponse);
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

    private async Task<bool> PullPowerwallPercentage(CollectorRegistry registry, LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/system_status/soe", loginResponse);
        if (metricsDocument is null)
        {
            return false;
        }

        CreateGauge(registry, "powerwall", "percentage").Set(metricsDocument.RootElement.GetProperty("percentage").GetDouble());
        return true;
    }

    private async Task<bool> PullSiteInfo(CollectorRegistry registry, LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/site_info", loginResponse);
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

    private async Task<bool> PullStatus(CollectorRegistry registry, LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/status", loginResponse);
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

    private async Task<bool> PullOperation(CollectorRegistry registry, LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/operation", loginResponse);
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

    private Gauge CreateGauge(CollectorRegistry registry, string category, string metric, params string[] labelNames)
        => Metrics.WithCustomRegistry(registry).CreateGauge($"tesla_gateway_{category}_{metric}", metric, labelNames);

    private async Task<JsonDocument?> CallMetricEndpointAsync(string path, LoginResponse loginResponse)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);
        using var response = await _client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Got {response.StatusCode} calling '{path}': {responseContent}");
            return null;
        }

        return JsonDocument.Parse(responseContent);
    }

    private void SetRequestDurationMetric(CollectorRegistry registry, bool loginCached, TimeSpan duration)
    {
        // Request duration metric
        var requestDurationGauge = CreateGauge(registry, "apiproxy", "request_duration_ms", "loginCached");
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
}