using System.Net;

namespace SolarGateway_PrometheusProxy.Support;

internal static class Extensions
{
    public static bool IsSuccessStatusCode(this HttpStatusCode statusCode)
        => (int)statusCode is >= 200 and < 300;

    public static bool IsAuthenticationFailure(this HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
}
