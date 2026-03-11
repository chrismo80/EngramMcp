using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using MemoryModel = EngramMcp.Core.Memory;

namespace EngramMcp.Infrastructure.Memory;

public sealed class CodeMemoryCatalog : IMemoryCatalog
{
    private static readonly IReadOnlyList<MemoryModel> Memories =
    [
        new("long-term", 100),
        new("medium-term", 25),
        new("short-term", 10),
    ];

    public IReadOnlyList<string> Names => Memories.Select(m => m.Name).ToList();
    
    public IReadOnlyList<MemoryModel> GetAll() => Memories;

    public MemoryModel GetByName(string name) =>
        Memories.FirstOrDefault(memory => string.Equals(memory.Name, name, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Unknown memory '{name}'.");
}
