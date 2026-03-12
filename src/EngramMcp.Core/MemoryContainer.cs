namespace EngramMcp.Core;

public sealed class MemoryContainer
{
    public Dictionary<string, List<MemoryEntry>> Memories { get; init; } = new(StringComparer.Ordinal);
}
