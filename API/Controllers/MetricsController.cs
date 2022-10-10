using Microsoft.AspNetCore.Mvc;
using Prometheus;
using TeslaGateway_PrometheusProxy.Filters;

namespace TeslaGateway_PrometheusProxy.Controllers;

[ApiController]
[Route("/metrics")]
[TypeFilter(typeof(MetricExceptionFilter))]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly CollectorRegistry _collectorRegistry;

    public MetricsController(
        IMetricsService metricsService,
        CollectorRegistry collectorRegistry)
        => (_metricsService, _collectorRegistry) = (metricsService, collectorRegistry);

    [HttpGet]
    public async Task GetMetrics()
    {
        await _metricsService.CollectMetricsAsync(_collectorRegistry, HttpContext.RequestAborted);

        // Serialize metrics
        await _collectorRegistry.CollectAndExportAsTextAsync(Response.Body);
    }
}