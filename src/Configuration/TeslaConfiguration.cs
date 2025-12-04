using SolarGateway_PrometheusProxy.Models;
using SolarGateway_PrometheusProxy.Support;

namespace SolarGateway_PrometheusProxy.Configuration;

public class TeslaConfiguration
{
    public bool Enabled { get; set; }

    /// <summary>
    /// See README
    /// </summary>
    [RequiredIf(nameof(Enabled), true)]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// See README
    /// </summary>
    [RequiredIf(nameof(Enabled), true)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// See README
    /// </summary>
    [RequiredIf(nameof(Enabled), true)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Is usually "customer"
    /// </summary>
    [RequiredIf(nameof(Enabled), true)]
    public string Username { get; set; } = "customer";

    /// <summary>
    /// Hostname to use in the request to the Tesla Gateway.
    /// There are specific values it accepts.
    /// If left unspecified, defaults to "powerwall".
    /// </summary>
    [RequiredIf(nameof(Enabled), true)]
    public string RequestHost { get; set; } = "powerwall";

    /// <summary>
    /// Timeout for requests to the Tesla Gateway.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// How long to cache a successful login check before pinging again.
    /// </summary>
    public int LoginCheckCacheSeconds { get; set; } = 5;

    /// <summary>
    /// How long to wait before retrying after a 429 Too Many Requests response.
    /// </summary>
    public int RateLimitBackoffSeconds { get; set; } = 15;

    public TeslaLoginRequest GetTeslaLoginRequest()
        => new()
        {
            Email = this.Email,
            Password = this.Password,
            Username = this.Username
        };
}