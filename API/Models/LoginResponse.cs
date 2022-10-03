using System.Text.Json.Serialization;

namespace TeslaGateway_PrometheusProxy.Models;

public class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
}