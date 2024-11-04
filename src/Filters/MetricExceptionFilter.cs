using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SolarGateway_PrometheusProxy.Exceptions;

namespace SolarGateway_PrometheusProxy.Filters;

public class MetricExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case MetricRequestFailedException ex:
                context.Result = new ObjectResult(new { error = ex.Message })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
                break;
            case OperationCanceledException:
                context.Result = new BadRequestObjectResult("Request cancelled");
                break;
        }
    }
}