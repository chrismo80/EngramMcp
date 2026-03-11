namespace EngramMcp.Core.Abstractions;

public interface IMemoryCatalog
{
    IReadOnlyList<MemoryModel> Memories { get; }

    MemoryModel GetByName(string name);
}
