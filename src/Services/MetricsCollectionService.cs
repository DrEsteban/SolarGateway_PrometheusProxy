using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Prometheus;
using SolarGateway_PrometheusProxy.Configuration;

namespace SolarGateway_PrometheusProxy.Services;

public class MetricsCollectionService(
    IEnumerable<IMetricsService> metricsServices,
    CollectorRegistry collectorRegistry,
    IMemoryCache cache,
    IOptions<ResponseCacheConfiguration> responseCacheConfiguration,
    ILogger<MetricsCollectionService> logger) : IMetricsCollectionService
{
    private readonly IEnumerable<IMetricsService> _metricsServices = metricsServices;
    private readonly CollectorRegistry _collectorRegistry = collectorRegistry;
    private readonly IMemoryCache _cache = cache;
    private readonly ResponseCacheConfiguration _configuration = responseCacheConfiguration.Value;
    private readonly ILogger<MetricsCollectionService> _logger = logger;

    private const string LastMetricsRequestCacheKey = "LastMetricsRequest";

    public async Task CollectAllAsync(CancellationToken cancellationToken = default)
    {
        // Create a linked token source with a timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(this._configuration.MetricsRequestTimeoutSeconds));

        // Log if we time out
        using var registration = cts.Token.Register(() => this._logger.LogWarning("Canceling metrics collection due to timeout"));

        await this._cache.GetOrCreateAsync(LastMetricsRequestCacheKey, async entry =>
        {
            await Task.WhenAll(this._metricsServices.Select(m => m.CollectMetricsAsync(this._collectorRegistry, cts.Token)));

            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(this._configuration.ResponseCacheDurationSeconds);
            return DateTimeOffset.UtcNow;
        });
    }
}
