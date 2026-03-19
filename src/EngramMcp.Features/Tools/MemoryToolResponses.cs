using System.Text.Json.Serialization;
using EngramMcp.Core;

namespace EngramMcp.Features.Tools;

public sealed record MemoryVisibleItemResponse
{
    [JsonPropertyName("memory")]
    public required string Text { get; init; }

    [JsonPropertyName("importance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Importance { get; init; }
}

public sealed record MemorySectionSummaryResponse
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("memories")]
    public required int EntryCount { get; init; }
}

public sealed record RecallResponse
{
    [JsonPropertyName("memories")]
    public required IReadOnlyDictionary<string, IReadOnlyList<MemoryVisibleItemResponse>> Memories { get; init; }

    [JsonPropertyName("customSections")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MemorySectionSummaryResponse>? CustomSections { get; init; }
}

public sealed record ReadSectionResponse
{
    [JsonPropertyName("memories")]
    public required IReadOnlyDictionary<string, IReadOnlyList<MemoryVisibleItemResponse>> Memories { get; init; }
}

public sealed record ConsolidateResponse
{
    [JsonPropertyName("section")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Section { get; init; }

    [JsonPropertyName("entries")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MaintenanceMemoryEntry>? Entries { get; init; }

    [JsonPropertyName("consolidationToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConsolidationToken { get; init; }

    [JsonPropertyName("failure")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MaintenanceSectionFailure? Failure { get; init; }
}
