using System.Text.Json.Serialization;

namespace TeslaGateway_PrometheusProxy.Models;

public class LoginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}