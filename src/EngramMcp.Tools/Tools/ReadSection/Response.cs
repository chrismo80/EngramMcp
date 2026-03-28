using System.Text.Json.Serialization;
using EngramMcp.Tools.Tools.Recall;

namespace EngramMcp.Tools.Tools.ReadSection;

public sealed record ReadSectionResponse
{
    [JsonPropertyName("memories")]
    public required IReadOnlyDictionary<string, IReadOnlyList<MemoryVisibleItemResponse>> Memories { get; init; }
}
