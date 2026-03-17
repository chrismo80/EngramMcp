using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.TestDoubles;

internal sealed class SpyMemoryService : IMemoryService
{
    public string? StoredName { get; private set; }

    public string? StoredText { get; private set; }

    public IReadOnlyList<string>? StoredTags { get; private set; }

    public MemoryImportance? StoredImportance { get; private set; }

    public string? ReadSection { get; private set; }

    public string? SearchQuery { get; private set; }

    public string? MaintenanceReadSection { get; private set; }

    public string? MaintenanceWriteSection { get; private set; }

    public string? MaintenanceWriteToken { get; private set; }

    public IReadOnlyList<MaintenanceMemoryEntry>? MaintenanceWriteEntries { get; private set; }

    public MemoryContainer RecallResult { get; init; } = new();

    public MemoryContainer ReadResult { get; init; } = new();

    public MaintenanceSectionReadResult MaintenanceReadResult { get; init; } = new()
    {
        Section = ShortTerm,
        Entries = [],
        MaintenanceToken = "token"
    };

    public MaintenanceSectionWriteResult MaintenanceWriteResult { get; init; } = new()
    {
        Section = ShortTerm,
        Entries = []
    };

    public IReadOnlyList<MemorySearchResult> SearchResult { get; init; } = [];

    public Exception? ReadException { get; init; }

    public Exception? SearchException { get; init; }

    public Exception? MaintenanceReadException { get; init; }

    public Exception? MaintenanceWriteException { get; init; }

    public Task StoreAsync(
        string section,
        string text,
        IReadOnlyList<string>? tags = null,
        MemoryImportance? importance = null,
        CancellationToken cancellationToken = default)
    {
        StoredName = section;
        StoredText = text;
        StoredTags = tags;
        StoredImportance = importance;
        return Task.CompletedTask;
    }

    public Task<MemoryContainer> ReadAsync(string section, CancellationToken cancellationToken = default)
    {
        ReadSection = section;

        return ReadException is null
            ? Task.FromResult(ReadResult)
            : Task.FromException<MemoryContainer>(ReadException);
    }

    public Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RecallResult);
    }

    public Task<MaintenanceSectionReadResult> ReadForMaintenanceAsync(string section, CancellationToken cancellationToken = default)
    {
        MaintenanceReadSection = section;

        return MaintenanceReadException is null
            ? Task.FromResult(MaintenanceReadResult)
            : Task.FromException<MaintenanceSectionReadResult>(MaintenanceReadException);
    }

    public Task<IReadOnlyList<MemorySearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        SearchQuery = query;

        return SearchException is null
            ? Task.FromResult(SearchResult)
            : Task.FromException<IReadOnlyList<MemorySearchResult>>(SearchException);
    }

    public Task<MaintenanceSectionWriteResult> WriteForMaintenanceAsync(
        string section,
        string maintenanceToken,
        IReadOnlyList<MaintenanceMemoryEntry> entries,
        CancellationToken cancellationToken = default)
    {
        MaintenanceWriteSection = section;
        MaintenanceWriteToken = maintenanceToken;
        MaintenanceWriteEntries = entries;

        return MaintenanceWriteException is null
            ? Task.FromResult(MaintenanceWriteResult)
            : Task.FromException<MaintenanceSectionWriteResult>(MaintenanceWriteException);
    }
}
