using EngramMcp.Tools.Memory.Storage;

namespace EngramMcp.Tools.Memory;

public sealed class CachedMemoryService(
    IMemoryStore memoryStore,
    IdGenerator memoryIdGenerator,
    RetentionPolicy retentionPolicy,
    SessionReinforcementTracker reinforcementTracker) : IMemoryService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private PersistedMemoryDocument? _document;

    public async Task<IReadOnlyList<RecallMemory>> RecallAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);

            if (PruneDeleteableMemories(document))
                await memoryStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            reinforcementTracker.Reset();
            return RecallMemories(document);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MemoryChangeResult> RememberAsync(RetentionTier retentionTier, string text, CancellationToken cancellationToken = default)
    {
        if (MemoryText.GetValidationError(text) is { } error)
            return MemoryChangeResult.Reject(error);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            var memory = new PersistedMemory
            {
                Id = memoryIdGenerator.GetUniqueId(),
                Text = text,
                Retention = retentionPolicy.CreateInitialRetention(retentionTier)
            };

            document.Memories.Add(memory);

            await memoryStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            return MemoryChangeResult.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MemoryChangeResult> ReinforceAsync(IReadOnlyList<string> memoryIds, CancellationToken cancellationToken = default)
    {
        if (memoryIds is null || memoryIds.Count == 0)
            return MemoryChangeResult.Reject("At least one memory id is required.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            var memoryIndexesById = document.Memories
                .Select((memory, index) => new KeyValuePair<string, int>(memory.Id, index))
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

            if (GetUnknownMemoryId(memoryIds, memoryIndexesById) is { } unknownMemoryId)
                return MemoryChangeResult.Reject($"Unknown memory '{unknownMemoryId}'.");

            var changedRetention = ApplyGlobalWeakeningIfFirstTime(document);
            changedRetention |= ReinforceMemories(document, memoryIndexesById, memoryIds.Distinct(StringComparer.Ordinal));

            if (changedRetention)
                await memoryStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            return MemoryChangeResult.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<PersistedMemoryDocument> LoadDocumentAsync(CancellationToken cancellationToken)
    {
        if (_document is not null)
            return _document;

        await memoryStore.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        _document = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        return _document;
    }

    private bool PruneDeleteableMemories(PersistedMemoryDocument document) =>
        document.Memories.RemoveAll(memory => retentionPolicy.ShouldDelete(memory.Retention)) > 0;

    private IReadOnlyList<RecallMemory> RecallMemories(PersistedMemoryDocument document) =>
        document.Memories
            .OrderByDescending(memory => memory.Retention)
            .Select(memory => new RecallMemory(memory.Id, memory.Text))
            .ToArray();

    private string? GetUnknownMemoryId(IReadOnlyList<string> memoryIds, IReadOnlyDictionary<string, int> memoryIndexesById)
    {
        foreach (var memoryId in memoryIds)
        {
            if (string.IsNullOrWhiteSpace(memoryId) || !memoryIndexesById.ContainsKey(memoryId))
                return memoryId;
        }

        return null;
    }

    private bool ApplyGlobalWeakeningIfFirstTime(PersistedMemoryDocument document)
    {
        if (!reinforcementTracker.MarkGlobalWeakeningAppliedIfFirstTime())
            return false;

        for (var index = 0; index < document.Memories.Count; index++)
        {
            var memory = document.Memories[index];
            document.Memories[index] = memory with { Retention = retentionPolicy.Decay(memory.Retention) };
        }

        return true;
    }

    private bool ReinforceMemories(PersistedMemoryDocument document, IReadOnlyDictionary<string, int> memoryIndexesById, IEnumerable<string> memoryIds)
    {
        var changedRetention = false;

        foreach (var memoryId in memoryIds)
        {
            if (!reinforcementTracker.MarkReinforcedIfFirstTime(memoryId))
                continue;

            var index = memoryIndexesById[memoryId];
            var memory = document.Memories[index];
            document.Memories[index] = memory with { Retention = retentionPolicy.Reinforce(memory.Retention) };
            changedRetention = true;
        }

        return changedRetention;
    }
}
