namespace EngramMcp.Core;

public sealed class MemoryDocument
{
    public Dictionary<string, List<MemoryEntry>> Memories { get; init; } = new(StringComparer.Ordinal);
}
