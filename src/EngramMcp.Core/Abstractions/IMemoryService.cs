namespace EngramMcp.Core.Abstractions;

public interface IMemoryService
{
    Task StoreAsync(
        string section,
        string text,
        IReadOnlyList<string>? tags = null,
        MemoryImportance? importance = null,
        CancellationToken cancellationToken = default);

    Task<MemoryContainer> ReadAsync(string section, CancellationToken cancellationToken = default);

    Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemorySearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
