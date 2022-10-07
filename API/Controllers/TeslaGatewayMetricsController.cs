using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace TeslaGateway_PrometheusProxy.Controllers;

[ApiController]
[Route("/metrics")]
public class TeslaGatewayMetricsController : ControllerBase
{
    private readonly TeslaGatewayMetricsService _metricsService;

    public TeslaGatewayMetricsController(TeslaGatewayMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var metrics = await _metricsService.CollectAndGetJsonAsync();
            return Content(metrics, MediaTypeNames.Application.Json);
        }
        catch (MetricRequestFailedException e)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = e.Message });
        }
    }
}