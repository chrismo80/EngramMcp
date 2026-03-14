using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class MemoryService(IMemoryCatalog memoryCatalog, IMemoryStore memoryStore)
    : IMemoryService
{
    public Task StoreAsync(
        string section,
        string text,
        IReadOnlyList<string>? tags = null,
        MemoryImportance? importance = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSection = NormalizeSectionIdentifier(section);

        return memoryStore.UpdateAsync(
            container =>
            {
                var resolvedSection = ResolveSectionName(normalizedSection, container);
                var memory = memoryCatalog.GetByName(resolvedSection);
                memory.Store(container, new MemoryEntry(CreateTimestamp(), text, tags, importance));
            },
            cancellationToken);
    }

    public async Task<MemoryContainer> ReadAsync(string section, CancellationToken cancellationToken = default)
    {
        var normalizedSection = NormalizeSectionIdentifier(section);

        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var resolvedSection = TryResolveExistingSectionName(normalizedSection, container);

        if (resolvedSection is not null)
            return CreateSectionDocument(resolvedSection, container.Memories.TryGetValue(resolvedSection, out var entries) ? entries : []);

        throw new KeyNotFoundException($"Memory section '{normalizedSection}' was not found. Available sections: {GetAvailableSectionNames(container)}.");
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
            .Except(memoryCatalog.Memories.Select(memory => memory.Name), StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        return string.Join(", ", builtInNames.Concat(customNames));
    }

    private List<MemorySectionSummary> GetCustomSectionSummaries(MemoryContainer container)
    {
        var builtInNames = memoryCatalog.Memories
            .Select(memory => memory.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return container.Memories
            .Where(pair => !builtInNames.Contains(pair.Key))
            .Select(pair => new MemorySectionSummary(pair.Key, pair.Value.Count))
            .OrderByDescending(summary => summary.EntryCount)
            .ThenBy(summary => summary.Name, StringComparer.Ordinal)
            .ToList();
    }

    private string ResolveSectionName(string requestedSection, MemoryContainer container)
    {
        var fixedMemory = memoryCatalog.GetByName(requestedSection);

        if (memoryCatalog.Memories.Any(memory => string.Equals(memory.Name, requestedSection, StringComparison.OrdinalIgnoreCase)))
            return fixedMemory.Name;

        return FindExistingCustomSectionName(requestedSection, container) ?? requestedSection;
    }

    private string? TryResolveExistingSectionName(string requestedSection, MemoryContainer container)
    {
        var fixedMemory = memoryCatalog.Memories.FirstOrDefault(memory => string.Equals(memory.Name, requestedSection, StringComparison.OrdinalIgnoreCase));

        if (fixedMemory is not null)
            return fixedMemory.Name;

        return FindExistingCustomSectionName(requestedSection, container);
    }

    private static string NormalizeSectionIdentifier(string? section)
    {
        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Memory section identifier must not be null, empty, or whitespace.", nameof(section));

        return section.Trim();
    }

    private static string? FindExistingCustomSectionName(string requestedSection, MemoryContainer container)
    {
        var matches = container.Memories.Keys
            .Where(name => string.Equals(name, requestedSection, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return matches.Length switch
        {
            0 => null,
            1 => matches[0],
            _ => throw new InvalidOperationException($"Memory store contains multiple sections that differ only by casing for '{requestedSection}'.")
        };
    }

    private static DateTime CreateTimestamp() => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
}
