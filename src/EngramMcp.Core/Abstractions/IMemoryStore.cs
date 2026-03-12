namespace EngramMcp.Core.Abstractions;

public interface IMemoryStore
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(Action<MemoryContainer> update, CancellationToken cancellationToken = default);

    Task<MemoryContainer> LoadAsync(CancellationToken cancellationToken = default);
}
