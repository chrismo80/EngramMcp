namespace EngramMcp.Tools.Memory;

public enum MemorySize
{
    Small,
    Normal,
    Big
}

public sealed class MemoryContainer
{
    public Dictionary<string, List<MemoryEntry>> Memories { get; init; } = new(StringComparer.Ordinal);

    public List<MemorySectionSummary> CustomSections { get; init; } = [];
}
