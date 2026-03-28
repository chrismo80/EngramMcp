namespace EngramMcp.Tools.Memory;

public sealed class MemorySection(string name, int capacity)
{
    public string Name { get; } = name;

    public int Capacity { get; } = capacity;

    public void Store(MemoryContainer container, MemoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(container);

        var entries = GetEntries(container);
        entries.Add(entry);

        while (entries.Count > Capacity)
            entries.Remove(entries.GetEntryToEvict());
    }

    public IReadOnlyList<MemoryEntry> Read(MemoryContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return GetEntries(container);
    }

    private List<MemoryEntry> GetEntries(MemoryContainer container)
    {
        if (container.Memories.TryGetValue(Name, out var entries))
            return entries;

        entries = [];
        container.Memories[Name] = entries;

        return entries;
    }
}
