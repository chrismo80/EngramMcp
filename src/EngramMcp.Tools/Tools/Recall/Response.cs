using System.Text.Json.Serialization;
using EngramMcp.Tools.Memory;

namespace EngramMcp.Tools.Tools.Recall;

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

internal static class RecallResponseFactory
{
    public static RecallResponse Create(MemoryContainer container)
    {
        return new RecallResponse
        {
            Memories = container.Memories.ToVisibleMemories(),
            CustomSections = container.CustomSections.Count == 0
                ? null
                : [.. container.CustomSections
                    .OrderByDescending(summary => summary.EntryCount)
                    .ThenBy(summary => summary.Name, StringComparer.Ordinal)
                    .Select(summary => new MemorySectionSummaryResponse
                    {
                        Name = summary.Name,
                        EntryCount = summary.EntryCount
                    })]
        };
    }

    internal static IReadOnlyDictionary<string, IReadOnlyList<MemoryVisibleItemResponse>> ToVisibleMemories(
        this IReadOnlyDictionary<string, List<MemoryEntry>> memories)
    {
        var visibleMemories = new Dictionary<string, IReadOnlyList<MemoryVisibleItemResponse>>(StringComparer.Ordinal);

        foreach (var memoryBlock in memories)
            visibleMemories[memoryBlock.Key] = [.. memoryBlock.Value.Select(ToVisibleItemResponse)];

        return visibleMemories;
    }

    private static MemoryVisibleItemResponse ToVisibleItemResponse(MemoryEntry memory)
    {
        return new MemoryVisibleItemResponse
        {
            Text = memory.Text,
            Importance = memory.Importance == MemoryImportance.High ? memory.Importance.ToSerializedValue() : null
        };
    }
}
