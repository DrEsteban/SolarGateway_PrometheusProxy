using Prometheus;
using SolarGateway_PrometheusProxy.Exceptions;
using SolarGateway_PrometheusProxy.Services;

namespace SolarGateway_PrometheusProxy;

public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this WebApplication app)
    {
        app.MapMethods("/metrics", [HttpMethods.Get, HttpMethods.Head], HandleMetricsRequest)
            .WithName("Metrics");
    }

    internal static async Task<IResult> HandleMetricsRequest(
        HttpContext context,
        IMetricsCollectionService metricsCollectionService,
        CollectorRegistry collectorRegistry,
        CancellationToken requestAborted)
    {
        try
        {
            await metricsCollectionService.CollectAllAsync(requestAborted);

            if (!HttpMethods.IsHead(context.Request.Method))
            {
                requestAborted.ThrowIfCancellationRequested();
                context.Response.ContentType = PrometheusConstants.ExporterOpenMetricsContentTypeValue.ToString();
                await collectorRegistry.CollectAndExportAsTextAsync(context.Response.Body, ExpositionFormat.OpenMetricsText, requestAborted);
            }

            return Results.Empty;
        }
        catch (MetricRequestFailedException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: ex.StatusCode);
        }
        catch (OperationCanceledException)
        {
            return Results.BadRequest("Request cancelled");
        }
    }
}
