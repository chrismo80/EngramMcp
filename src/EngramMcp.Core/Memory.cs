namespace EngramMcp.Core;

public sealed class Memory(string name, int capacity)
{
    public string Name { get; } = name;

    public int Capacity { get; } = capacity;

    public void Store(MemoryDocument document, MemoryEntry entry)
    {
        // TODO(code-monkey): Append the entry to this memory's list in the document and enforce
        // FIFO eviction according to Capacity.
        throw new NotImplementedException();
    }

    public IReadOnlyList<MemoryEntry> Read(MemoryDocument document)
    {
        // TODO(code-monkey): Return the entries for this memory name from the document, preserving
        // their current order and treating the memory name as the lookup key.
        throw new NotImplementedException();
    }
}
