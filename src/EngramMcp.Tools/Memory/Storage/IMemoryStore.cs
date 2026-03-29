namespace EngramMcp.Tools.Memory.Storage;

public interface IMemoryStore
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
    Task<PersistedMemoryDocument> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(PersistedMemoryDocument document, CancellationToken cancellationToken = default);
}
