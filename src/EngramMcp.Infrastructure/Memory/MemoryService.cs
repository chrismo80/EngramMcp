using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class MemoryService(IMemoryCatalog memoryCatalog, IMemoryFileStore fileStore)
    : IMemoryService
{
    public async Task StoreAsync(string memoryName, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Memory text must not be null, empty, or whitespace.", nameof(text));
        }

        var memory = memoryCatalog.GetByName(memoryName);

        await fileStore.UpdateAsync(
            document => memory.Store(document, new MemoryEntry(CreateTimestamp(), text)),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<MemoryDocument> RecallAsync(CancellationToken cancellationToken = default)
    {
        var document = await fileStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var recalled = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal);

        foreach (var memory in memoryCatalog.GetAll())
        {
            recalled[memory.Name] = [.. memory.Read(document)];
        }

        return new MemoryDocument { Memories = recalled };
    }

    private static DateTime CreateTimestamp()
    {
        return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    }
}
