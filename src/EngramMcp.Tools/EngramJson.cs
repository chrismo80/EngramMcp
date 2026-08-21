using System.Text.Json;
using System.Text.Json.Serialization;

namespace EngramMcp.Tools;

public static class EngramJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
