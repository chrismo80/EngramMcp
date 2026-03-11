namespace EngramMcp.Core.Abstractions;

public interface IMemoryFileStore
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    Task<MemoryDocument> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(MemoryDocument document, CancellationToken cancellationToken = default);
}
