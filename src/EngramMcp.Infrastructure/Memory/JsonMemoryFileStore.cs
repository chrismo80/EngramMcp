using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class JsonMemoryFileStore(string filePath) : IMemoryFileStore
{
    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        // TODO(code-monkey): Validate the configured --file path, create parent directories if needed,
        // create the memory file when missing, and fail clearly on invalid or inaccessible locations.
        throw new NotImplementedException();
    }

    public Task<MemoryDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        // TODO(code-monkey): Load and deserialize the JSON memory document, validate that the
        // expected top-level sections exist, and fail loudly on malformed JSON or invalid structure.
        throw new NotImplementedException();
    }

    public Task SaveAsync(MemoryDocument document, CancellationToken cancellationToken = default)
    {
        // TODO(code-monkey): Persist the full memory document back to the configured file using
        // the agreed human-readable JSON shape.
        throw new NotImplementedException();
    }
}
