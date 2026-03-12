using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class CodeMemoryCatalog : IMemoryCatalog
{
    public IReadOnlyList<MemorySection> Memories { get; } =
    [
        new("long-term", 40),
        new("medium-term", 20),
        new("short-term", 10),
    ];
    
    public MemorySection GetByName(string name) => Memories.SingleOrDefault(memory => string.Equals(memory.Name, name, StringComparison.Ordinal))
                                                 ?? throw new InvalidOperationException($"Unknown memory '{name}'.");
}
