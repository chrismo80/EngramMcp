using System.Text.Json.Serialization;

namespace EngramMcp.Core;

public sealed record MaintenanceMemoryEntry
{
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("importance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Importance { get; init; }
}
