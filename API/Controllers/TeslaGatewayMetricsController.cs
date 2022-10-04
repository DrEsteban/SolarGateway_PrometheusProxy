using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Prometheus;
using TeslaGateway_PrometheusProxy.Models;

namespace TeslaGateway_PrometheusProxy.Controllers;

[ApiController]
[Route("/metrics")]
public class TeslaGatewayMetricsController : ControllerBase
{
    private readonly LoginRequest _loginRequest;
    private readonly TeslaGatewaySettings _gatewaySettings;
    private readonly ILogger<TeslaGatewayMetricsController> _logger;
    private readonly CollectorRegistry _registry;
    private readonly IMemoryCache _cache;

    public TeslaGatewayMetricsController(
        IOptions<LoginRequest> loginRequest,
        IOptions<TeslaGatewaySettings> gatewaySettings,
        ILogger<TeslaGatewayMetricsController> logger,
        CollectorRegistry registry,
        IMemoryCache cache)
    {
        _loginRequest = loginRequest.Value;
        _gatewaySettings = gatewaySettings.Value;
        _logger = logger;
        _registry = registry;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics()
    {
        // Get token
        var loginResponse = await _cache.GetOrCreateAsync("gateway_token", async e =>
        {
            var (success, response) = await CurlExecutor.ExecuteCurlAsync(BuildUri("/api/login/Basic"), _loginRequest);
            if (!success)
            {
                _logger.LogError($"Error calling login endpoint: {response}");
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1);
                return null;
            }
            
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return JsonSerializer.Deserialize<LoginResponse>(response);
        });

        if (loginResponse is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (string.IsNullOrEmpty(loginResponse?.Token))
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            _logger.LogError($"Failed to parse {nameof(LoginResponse)} for valid token");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        // Get metrics
        var results = await Task.WhenAll(
            PullMeterAggregates(loginResponse),
            PullPowerwallPercentage(loginResponse),
            PullSiteInfo(loginResponse),
            PullStatus(loginResponse),
            PullOperation(loginResponse));
        if (!results.All(r => r))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = $"Failed to pull {results.Count(r => !r)}/{results.Length} endpoints on gateway" });
        }
        
        await using var stream = new MemoryStream();
        await _registry.CollectAndExportAsTextAsync(stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return Content(await reader.ReadToEndAsync(), MediaTypeNames.Application.Json);
    }

    private async Task<bool> PullMeterAggregates(LoginResponse loginResponse)
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
                        CreateGauge(category.Name, metric.Name).Set(metric.Value.GetDouble());
                        break;
                    case JsonValueKind.String:
                        // Assumed to be DateTime
                        if (DateTimeOffset.TryParse(metric.Value.GetString(), out var date))
                        {
                            CreateGauge(category.Name, metric.Name).Set(date.SecondsSinceEpoch());
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

    private async Task<bool> PullPowerwallPercentage(LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/system_status/soe", loginResponse);
        if (metricsDocument is null)
        {
            return false;
        }

        CreateGauge("powerwall", "percentage").Set(metricsDocument.RootElement.GetProperty("percentage").GetDouble());
        return true;
    }

    private async Task<bool> PullSiteInfo(LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/site_info", loginResponse);
        if (metricsDocument is null)
        {
            return false;
        }

        foreach (var metric in metricsDocument.RootElement.EnumerateObject()
                     .Where(p => p.Value.ValueKind == JsonValueKind.Number))
        {
            CreateGauge("siteinfo", metric.Name).Set(metric.Value.GetDouble());
        }

        return true;
    }

    private static Regex UpTimeRegex = new Regex("^(?<hours>[0-9]*)h(?<minutes>[0-9]*)m(?<seconds>[0-9]*)(\\.[0-9]*s)?$", RegexOptions.Compiled);

    private async Task<bool> PullStatus(LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/status", loginResponse);
        if (metricsDocument is null)
        {
            return false;
        }

        if (DateTimeOffset.TryParse(metricsDocument.RootElement.GetProperty("start_time").GetString(), out var startTime))
        {
            CreateGauge("status", "start_time").Set(startTime.SecondsSinceEpoch());
        }

        var match = UpTimeRegex.Match(metricsDocument.RootElement.GetProperty("up_time_seconds").GetString() ?? string.Empty);
        if (match.Success)
        {
            int hours = int.Parse(match.Groups["hours"].Value);
            int minutes = int.Parse(match.Groups["minutes"].Value);
            int seconds = int.Parse(match.Groups["seconds"].Value);
            var timeSpan = new TimeSpan(hours, minutes, seconds);
            CreateGauge("status", "up_time_seconds").Set(timeSpan.TotalSeconds);
        }

        return true;
    }

    private async Task<bool> PullOperation(LoginResponse loginResponse)
    {
        var metricsDocument = await CallMetricEndpointAsync("/api/operation", loginResponse);
        if (metricsDocument is null)
        {
            return false;
        }

        CreateGauge("operation", "backup_reserve_percent").Set(metricsDocument.RootElement.GetProperty("backup_reserve_percent").GetDouble());
        
        string? mode = metricsDocument.RootElement.GetProperty("real_mode").GetString();
        var modeGauge = CreateGauge("operation", "mode", "mode");

        const string selfConsumption = "self_consumption", autonomous = "autonomous", backup = "backup";
        modeGauge.WithLabels(selfConsumption).Set(mode == selfConsumption ? 1 : 0);
        modeGauge.WithLabels(autonomous).Set(mode == autonomous ? 1 : 0);
        modeGauge.WithLabels(backup).Set(mode == backup ? 1 : 0);

        return true;
    }

    private Gauge CreateGauge(string category, string metric, params string[] labelNames)
        => Metrics.WithCustomRegistry(_registry).CreateGauge($"tesla_gateway_{category}_{metric}", metric, labelNames);

    private async Task<JsonDocument?> CallMetricEndpointAsync(string path, LoginResponse loginResponse)
    {
        var (success, response) = await CurlExecutor.ExecuteCurlAsync(BuildUri(path), loginResponse.Token);
        if (!success)
        {
            _logger.LogError($"Error calling '{path}': {response}");
            return null;
        }

        return JsonDocument.Parse(response);
    }

    private string BuildUri(string path)
        => new UriBuilder("https", _gatewaySettings.Host, 443, path).ToString();
}