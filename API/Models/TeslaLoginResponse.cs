using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace SolarGateway_PrometheusProxy.Models;

public class TeslaLoginResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    public AuthenticationHeaderValue ToAuthenticationHeader()
        => new AuthenticationHeaderValue("Bearer", this.Token);
}