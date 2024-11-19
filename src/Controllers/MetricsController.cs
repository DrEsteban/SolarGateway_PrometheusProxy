using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Prometheus;
using SolarGateway_PrometheusProxy.Filters;
using SolarGateway_PrometheusProxy.Models;

namespace SolarGateway_PrometheusProxy.Controllers;

[ApiController]
[Route("/metrics")]
[TypeFilter(typeof(MetricExceptionFilter))]
public class MetricsController(
    IEnumerable<IMetricsService> metricsService,
    CollectorRegistry collectorRegistry,
    IOptions<ResponseCacheConfiguration> responseCacheConfiguration,
    IMemoryCache cache) : ControllerBase
{
    private readonly IEnumerable<IMetricsService> _metricsServices = metricsService;
    private readonly CollectorRegistry _collectorRegistry = collectorRegistry;
    private readonly ResponseCacheConfiguration _responseCacheConfiguration = responseCacheConfiguration.Value;
    private readonly IMemoryCache _cache = cache;

    [HttpHead("/health")] // health check
    [HttpHead] // health check
    [HttpGet]
    [ResponseCache(CacheProfileName = "default")]
    public async Task GetMetrics()
    {
        // Ensure metrics are only collected once per cache duration
        await this._cache.GetOrCreateAsync("LastMetricsRequest",
            async e =>
            {
                await Task.WhenAll(_metricsServices.Select(m => m.CollectMetricsAsync(_collectorRegistry, HttpContext.RequestAborted)));
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_responseCacheConfiguration.ResponseCacheDurationSeconds);
                return DateTimeOffset.UtcNow;
            });

        if (this.Request.Method != HttpMethods.Head)
        {
            HttpContext.RequestAborted.ThrowIfCancellationRequested();

            // Serialize metrics
            Response.ContentType = PrometheusConstants.ExporterOpenMetricsContentTypeValue.ToString();
            await _collectorRegistry.CollectAndExportAsTextAsync(Response.Body, ExpositionFormat.OpenMetricsText, HttpContext.RequestAborted);
        }
    }
}