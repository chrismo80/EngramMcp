using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class MemoryService(IMemoryCatalog memoryCatalog, IMemoryFileStore fileStore)
    : IMemoryService
{
    public Task StoreAsync(string memoryName, string text, CancellationToken cancellationToken = default)
    {
        // TODO(code-monkey): Reject null/empty/whitespace text, load the document, resolve the target
        // memory from the catalog by name, and delegate storing/FIFO behavior to that memory instance.
        throw new NotImplementedException();
    }

    public Task<MemoryDocument> RecallAsync(CancellationToken cancellationToken = default)
    {
        // TODO(code-monkey): Load the document, iterate over all configured memories from the catalog,
        // call Read on each memory, and return a raw name-keyed memory document.
        throw new NotImplementedException();
    }
}
