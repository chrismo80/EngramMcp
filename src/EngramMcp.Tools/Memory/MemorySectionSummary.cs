namespace EngramMcp.Tools.Memory;

public sealed class MemorySectionSummary(string name, int entryCount)
{
    public string Name { get; } = name;

    public int EntryCount { get; } = entryCount;
}
