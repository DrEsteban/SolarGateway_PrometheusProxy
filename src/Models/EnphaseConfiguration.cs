using SolarGateway_PrometheusProxy.Support;

namespace SolarGateway_PrometheusProxy.Models;

public class EnphaseConfiguration
{
    public bool Enabled { get; set; }

    [RequiredIf(nameof(Enabled), true)]
    public string Host { get; set; } = string.Empty;
}
