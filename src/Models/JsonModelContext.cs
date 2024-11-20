using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolarGateway_PrometheusProxy.Models;

[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(TeslaLoginRequest))]
[JsonSerializable(typeof(TeslaLoginResponse))]
[JsonSerializable(typeof(IDictionary<string, string[]>), TypeInfoPropertyName = "StringArrayDictionary")]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
public sealed partial class JsonModelContext : JsonSerializerContext
{
    static JsonModelContext()
    {
        Default = new JsonModelContext(new JsonSerializerOptions(s_defaultOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        Indented = new JsonModelContext(new JsonSerializerOptions(s_defaultOptions)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public static JsonModelContext Indented { get; }
}
