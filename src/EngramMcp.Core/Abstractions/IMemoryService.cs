namespace EngramMcp.Core.Abstractions;

public interface IMemoryService
{
    Task StoreAsync(string memoryName, string text, CancellationToken cancellationToken = default);

    Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default);
}
