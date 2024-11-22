using System.Text.Json.Serialization;

namespace SolarGateway_PrometheusProxy.Models;

public class TeslaLoginRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("password")]
    public required string Password { get; set; }

    [JsonPropertyName("username")]
    public required string Username { get; set; }
}