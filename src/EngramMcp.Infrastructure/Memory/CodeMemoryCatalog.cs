using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Infrastructure.Memory;

public sealed class CodeMemoryCatalog : IMemoryCatalog
{
    private const int CustomMemoryCapacity = 20;

    private readonly IReadOnlyDictionary<string, MemorySection> _fixedMemories;

    public IReadOnlyList<MemorySection> Memories { get; } =
    [
        new(LongTerm, 20),
        new(MediumTerm, 10),
        new(ShortTerm, 5),
    ];

    public CodeMemoryCatalog()
    {
        _fixedMemories = Memories.ToDictionary(memory => memory.Name, StringComparer.OrdinalIgnoreCase);
    }

    public MemorySection GetByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _fixedMemories.TryGetValue(name, out var memory) ? memory : new MemorySection(name, CustomMemoryCapacity);
    }

    public IReadOnlyList<MemorySection> GetRecallOrder(MemoryContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return Memories;
    }
}