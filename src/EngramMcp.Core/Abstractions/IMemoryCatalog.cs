namespace EngramMcp.Core.Abstractions;

public interface IMemoryCatalog
{
    IReadOnlyList<string> Names { get; }
    
    IReadOnlyList<Memory> GetAll();

    Memory GetByName(string name);
}
