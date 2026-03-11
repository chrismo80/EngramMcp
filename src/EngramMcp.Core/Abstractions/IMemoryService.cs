namespace EngramMcp.Core.Abstractions;

public interface IMemoryService
{
    Task StoreAsync(string memoryName, string text, CancellationToken cancellationToken = default);

    Task<MemoryDocument> RecallAsync(CancellationToken cancellationToken = default);
}
