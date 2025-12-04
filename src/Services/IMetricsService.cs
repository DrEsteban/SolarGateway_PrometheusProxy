using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;

namespace SolarGateway_PrometheusProxy.Services;

/// <summary>
/// Implemented by services that collect metrics from a solar brand's device(s) and save them to a Prometheus <see cref="CollectorRegistry"/>.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Collects metrics from the solar gateway and applies them to the <paramref name="collectorRegistry"/>.
    /// </summary>
    /// <remarks>
    /// Implementations are not expected to be thread-safe. The caller is responsible for ensuring that only one thread calls this method at a time.
    /// </remarks>
    /// <exception cref="MetricRequestFailedException">Thrown when the solar gateway returns an unexpected response.</exception>
    /// <exception cref="Exception"></exception>
    Task CollectMetricsAsync(CollectorRegistry collectorRegistry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the integration has a valid authentication context for future metric pulls.
    /// </summary>
    Task EnsureLoggedInAsync(CancellationToken cancellationToken = default);
}