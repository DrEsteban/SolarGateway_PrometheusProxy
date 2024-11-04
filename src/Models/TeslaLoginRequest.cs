using System.Text.Json.Serialization;

namespace SolarGateway_PrometheusProxy.Models;

public class TeslaLoginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}