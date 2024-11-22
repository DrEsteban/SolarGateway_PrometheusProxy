using SolarGateway_PrometheusProxy.Support;

namespace SolarGateway_PrometheusProxy.Models;

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
    /// How long to cache the login before fetching a new one
    /// </summary>
    public int LoginCacheMinutes { get; set; } = 10;

    public TeslaLoginRequest GetTeslaLoginRequest()
        => new()
        {
            Email = this.Email,
            Password = this.Password,
            Username = this.Username
        };
}