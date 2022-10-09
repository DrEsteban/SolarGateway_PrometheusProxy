using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeslaGateway_PrometheusProxy.Filters;

public class MetricExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is MetricRequestFailedException ex)
        {
            context.Result = new ObjectResult(new { error = ex.Message })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
    }
}