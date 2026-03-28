namespace EngramMcp.Tools.Memory;

public interface IMemoryCatalog
{
    IReadOnlyList<MemorySection> Memories { get; }

    MemorySection GetByName(string name);

    IReadOnlyList<MemorySection> GetRecallOrder(MemoryContainer container);
}
