namespace SolarGateway_PrometheusProxy;

public static class Extensions
{
    public static double SecondsSinceEpoch(this DateTimeOffset dateTime)
        => (dateTime - DateTimeOffset.UnixEpoch).TotalSeconds;
}