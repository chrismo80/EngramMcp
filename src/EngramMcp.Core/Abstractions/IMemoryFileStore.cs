namespace EngramMcp.Core.Abstractions;

public interface IMemoryFileStore
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(Action<MemoryDocument> update, CancellationToken cancellationToken = default);

    Task<MemoryDocument> LoadAsync(CancellationToken cancellationToken = default);
}
