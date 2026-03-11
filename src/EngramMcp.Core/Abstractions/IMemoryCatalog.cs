namespace EngramMcp.Core.Abstractions;

public interface IMemoryCatalog
{
    IReadOnlyList<Memory> GetAll();

    Memory GetByName(string name);
}
