using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class MemoryService(IMemoryCatalog memoryCatalog, IMemoryStore memoryStore)
    : IMemoryService
{
    public async Task StoreAsync(string section, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Memory text must not be null, empty, or whitespace.", nameof(text));

        var memory = memoryCatalog.GetByName(section);

        await memoryStore
            .UpdateAsync(container => memory.Store(container, new MemoryEntry(CreateTimestamp(), text)), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MemoryContainer> ReadAsync(string section, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var fixedMemory = memoryCatalog.Memories.FirstOrDefault(memory => string.Equals(memory.Name, section, StringComparison.Ordinal));

        if (fixedMemory is not null)
            return CreateSectionDocument(section, container.Memories.TryGetValue(section, out var entries) ? entries : []);

        if (container.Memories.TryGetValue(section, out var customEntries))
            return CreateSectionDocument(section, customEntries);

        throw new KeyNotFoundException($"Memory section '{section}' was not found.");
    }

    public async Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default)
    {
        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);

        var recalled = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal);

        foreach (var memory in memoryCatalog.GetRecallOrder(container))
            recalled[memory.Name] = [.. memory.Read(container)];

        return new MemoryContainer { Memories = recalled };
    }

    private static MemoryContainer CreateSectionDocument(string section, IReadOnlyList<MemoryEntry> entries)
    {
        return new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [section] = [.. entries]
            }
        };
    }

    private static DateTime CreateTimestamp() => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
}
