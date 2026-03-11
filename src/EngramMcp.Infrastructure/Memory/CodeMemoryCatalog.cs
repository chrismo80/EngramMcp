using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using MemoryModel = EngramMcp.Core.Memory;

namespace EngramMcp.Infrastructure.Memory;

public sealed class CodeMemoryCatalog : IMemoryCatalog
{
    private static readonly IReadOnlyList<MemoryModel> Memories =
    [
        new("shortTerm", 10),
        new("mediumTerm", 25),
        new("longTerm", 100)
    ];

    public IReadOnlyList<MemoryModel> GetAll() => Memories;

    public MemoryModel GetByName(string name) =>
        Memories.FirstOrDefault(memory => string.Equals(memory.Name, name, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Unknown memory '{name}'.");
}
