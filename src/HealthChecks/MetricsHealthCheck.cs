using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolarGateway_PrometheusProxy.Services;

namespace SolarGateway_PrometheusProxy.HealthChecks;

public class MetricsHealthCheck(IMetricsCollectionService metricsCollectionService) : IHealthCheck
{
    private readonly IMetricsCollectionService _metricsCollectionService = metricsCollectionService;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await this._metricsCollectionService.CollectAllAsync(cancellationToken);
            return HealthCheckResult.Healthy("Metrics collection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Metrics collection failed", ex);
        }
    }
}
