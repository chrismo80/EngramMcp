namespace EngramMcp.Core.Abstractions;

public interface IMemoryCatalog
{
    IReadOnlyList<MemorySection> Memories { get; }

    MemorySection GetByName(string name);
}
