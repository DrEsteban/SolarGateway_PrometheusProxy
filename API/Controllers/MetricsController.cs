using Microsoft.AspNetCore.Mvc;
using Prometheus;
using SolarGateway_PrometheusProxy.Filters;

namespace SolarGateway_PrometheusProxy.Controllers;

[ApiController]
[Route("/metrics")]
[TypeFilter(typeof(MetricExceptionFilter))]
public class MetricsController : ControllerBase
{
    private readonly IEnumerable<IMetricsService> _metricsServices;
    private readonly CollectorRegistry _collectorRegistry;

    public MetricsController(
        IEnumerable<IMetricsService> metricsService,
        CollectorRegistry collectorRegistry)
        => (_metricsServices, _collectorRegistry) = (metricsService, collectorRegistry);

    [HttpGet]
    [ResponseCache(CacheProfileName = "default")]
    public async Task GetMetrics()
    {
        await Task.WhenAll(_metricsServices.Select(m => m.CollectMetricsAsync(_collectorRegistry, HttpContext.RequestAborted)));
        HttpContext.RequestAborted.ThrowIfCancellationRequested();

        // Serialize metrics
        await _collectorRegistry.CollectAndExportAsTextAsync(Response.Body);
    }
}