using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class MemoryService(IMemoryCatalog memoryCatalog, IMemoryStore memoryStore)
    : IMemoryService
{
    public async Task StoreAsync(string memoryName, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Memory text must not be null, empty, or whitespace.", nameof(text));

        var memory = memoryCatalog.GetByName(memoryName);

        await memoryStore
            .UpdateAsync(container => memory.Store(container, new MemoryEntry(CreateTimestamp(), text)), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default)
    {
        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        
        var recalled = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal);

        foreach (var memory in memoryCatalog.Memories)
            recalled[memory.Name] = [.. memory.Read(container)];

        return new MemoryContainer { Memories = recalled };
    }

    private static DateTime CreateTimestamp() => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
}
