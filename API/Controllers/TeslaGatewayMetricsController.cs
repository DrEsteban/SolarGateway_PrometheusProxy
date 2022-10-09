using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using TeslaGateway_PrometheusProxy.Filters;

namespace TeslaGateway_PrometheusProxy.Controllers;

[ApiController]
[Route("/metrics")]
[TypeFilter(typeof(MetricExceptionFilter))]
public class TeslaGatewayMetricsController : ControllerBase
{
    private readonly TeslaGatewayMetricsService _metricsService;

    public TeslaGatewayMetricsController(TeslaGatewayMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet]
    public Task<string> GetMetrics()
        => _metricsService.CollectAndSerializeMetricsAsync();
}