using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolarGateway_PrometheusProxy.Models;

[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(TeslaLoginRequest))]
[JsonSerializable(typeof(TeslaLoginResponse))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true, 
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class JsonModelContext : JsonSerializerContext
{
}
