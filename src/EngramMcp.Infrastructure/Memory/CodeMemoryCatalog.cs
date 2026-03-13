using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class CodeMemoryCatalog : IMemoryCatalog
{
    private const int CustomMemoryCapacity = 50;

    private readonly IReadOnlyDictionary<string, MemorySection> _fixedMemories;

    public CodeMemoryCatalog()
    {
        Memories =
        [
            new("long-term", 40),
            new("medium-term", 20),
            new("short-term", 10),
        ];

        _fixedMemories = Memories.ToDictionary(memory => memory.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<MemorySection> Memories { get; }

    public MemorySection GetByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _fixedMemories.TryGetValue(name, out var memory)
            ? memory
            : new MemorySection(name, CustomMemoryCapacity);
    }

    public IReadOnlyList<MemorySection> GetRecallOrder(MemoryContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return Memories;
    }
}
