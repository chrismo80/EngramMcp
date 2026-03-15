using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Infrastructure.Memory;

public enum MemorySize
{
    Small,
    Normal,
    Big
}

public sealed class CodeMemoryCatalog : IMemoryCatalog
{
    private readonly int _baseCapacity;

    private readonly IReadOnlyDictionary<string, MemorySection> _fixedMemories;

    public IReadOnlyList<MemorySection> Memories { get; }

    public CodeMemoryCatalog(MemorySize size)
    {
        _baseCapacity = GetBaseCapacity(size);
        Memories =
        [
            new(LongTerm, _baseCapacity * 4),
            new(MediumTerm, _baseCapacity * 2),
            new(ShortTerm, _baseCapacity),
        ];
        _fixedMemories = Memories.ToDictionary(memory => memory.Name, StringComparer.OrdinalIgnoreCase);
    }

    public MemorySection GetByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if(_fixedMemories.TryGetValue(name, out var memory))
            return memory;

        return new MemorySection(name, _baseCapacity * 4);
    }

    public IReadOnlyList<MemorySection> GetRecallOrder(MemoryContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return Memories;
    }

    private static int GetBaseCapacity(MemorySize size) => size switch
    {
        MemorySize.Small => 5,
        MemorySize.Normal => 10,
        MemorySize.Big => 20,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported memory size.")
    };
}