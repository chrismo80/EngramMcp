using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class MemoryService(IMemoryCatalog memoryCatalog, IMemoryStore memoryStore)
    : IMemoryService
{
    public async Task StoreAsync(string section, string text, CancellationToken cancellationToken = default)
    {
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

        throw new KeyNotFoundException($"Memory section '{section}' was not found. Available sections: {GetAvailableSectionNames(container)}.");
    }

    public async Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default)
    {
        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);

        var recalled = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal);

        foreach (var memory in memoryCatalog.GetRecallOrder(container))
            recalled[memory.Name] = [.. memory.Read(container)];

        return new MemoryContainer
        {
            Memories = recalled,
            CustomSections = GetCustomSectionSummaries(container)
        };
    }

    public async Task<IReadOnlyList<MemorySearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query must not be null, empty, or whitespace.", nameof(query));

        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);

        return container.Memories
            .SelectMany(section => section.Value.Select(entry => new MemorySearchResult(section.Key, entry)))
            .Where(result => Matches(result, query))
            .OrderByDescending(result => result.Entry.Importance)
            .ThenByDescending(result => result.Entry.Timestamp)
            .ToList();
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

    private static bool Matches(MemorySearchResult result, string query)
    {
        return result.Section.Contains(query, StringComparison.OrdinalIgnoreCase)
            || result.Entry.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
            || result.Entry.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private string GetAvailableSectionNames(MemoryContainer container)
    {
        var builtInNames = memoryCatalog.Memories.Select(memory => memory.Name);
        var customNames = container.Memories.Keys
            .Except(memoryCatalog.Memories.Select(memory => memory.Name), StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);

        return string.Join(", ", builtInNames.Concat(customNames));
    }

    private List<MemorySectionSummary> GetCustomSectionSummaries(MemoryContainer container)
    {
        var builtInNames = memoryCatalog.Memories
            .Select(memory => memory.Name)
            .ToHashSet(StringComparer.Ordinal);

        return container.Memories
            .Where(pair => !builtInNames.Contains(pair.Key))
            .Select(pair => new MemorySectionSummary(pair.Key, pair.Value.Count))
            .OrderByDescending(summary => summary.EntryCount)
            .ThenBy(summary => summary.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static DateTime CreateTimestamp() => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
}
