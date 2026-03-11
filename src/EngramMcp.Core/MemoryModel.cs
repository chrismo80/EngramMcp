namespace EngramMcp.Core;

public sealed class MemoryModel(string name, int capacity)
{
    public string Name { get; } = name;

    public int Capacity { get; } = capacity;

    public void Store(MemoryDocument document, MemoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(document);

        var entries = GetEntries(document);
        entries.Add(entry);

        while (entries.Count > Capacity)
            entries.RemoveAt(0);
    }

    public IReadOnlyList<MemoryEntry> Read(MemoryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        
        return GetEntries(document);
    }

    private List<MemoryEntry> GetEntries(MemoryDocument document)
    {
        if (document.Memories.TryGetValue(Name, out var entries))
            return entries;

        entries = [];
        document.Memories[Name] = entries;
        
        return entries;
    }
}
