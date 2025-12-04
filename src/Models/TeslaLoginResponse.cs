using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace SolarGateway_PrometheusProxy.Models;

public class TeslaLoginResponse
{
    private string? _token;
    [JsonPropertyName("token")]
    [JsonRequired]
    public string? Token
    {
        get => this._token;
        set
        {
            this._token = value;
            this.AuthenticationHeader = value is null ? null : new("Bearer", value);
        }
    }

    [JsonIgnore]
    public AuthenticationHeaderValue? AuthenticationHeader { get; private set; }
}