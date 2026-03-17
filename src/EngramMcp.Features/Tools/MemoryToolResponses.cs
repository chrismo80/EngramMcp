using System.Text.Json.Serialization;
using EngramMcp.Core;

namespace EngramMcp.Features.Tools;

public sealed record MemoryVisibleItemResponse
{
    [JsonPropertyName("memory")]
    public required string Text { get; init; }

    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Tags { get; init; }

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

public sealed record SearchItemResponse
{
    [JsonPropertyName("memory")]
    public required string Text { get; init; }

    [JsonPropertyName("section")]
    public required string Section { get; init; }

    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("importance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Importance { get; init; }
}

public sealed record SearchResponse
{
    [JsonPropertyName("results")]
    public required IReadOnlyList<SearchItemResponse> Results { get; init; }
}

public sealed record MaintainSectionResponse
{
    [JsonPropertyName("section")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Section { get; init; }

    [JsonPropertyName("entries")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MaintenanceMemoryEntry>? Entries { get; init; }

    [JsonPropertyName("maintenanceToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MaintenanceToken { get; init; }

    [JsonPropertyName("failure")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MaintenanceSectionFailure? Failure { get; init; }
}
