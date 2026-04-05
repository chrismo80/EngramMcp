using EngramMcp.Tools.Memory.Storage;

namespace EngramMcp.Tools.Memory;

public sealed class MemoryService(
    GlobalJsonMemoryStore globalStore,
    ProjectJsonMemoryStore projectStore,
    RetentionPolicy retentionPolicy,
    Tracker tracker)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<RecallMemory>> RecallAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var globalDocument = await globalStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            var projectDocument = await projectStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            if (PruneDeleteableMemories(globalDocument))
                await globalStore.SaveAsync(globalDocument, cancellationToken).ConfigureAwait(false);

            if (PruneDeleteableMemories(projectDocument))
                await projectStore.SaveAsync(projectDocument, cancellationToken).ConfigureAwait(false);

            tracker.Reset();
            return RecallMemories(globalDocument, projectDocument);
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
            IMemoryStore store = retentionTier == RetentionTier.Short ? projectStore : globalStore;
            var scopePrefix = retentionTier == RetentionTier.Short ? "p-" : "g-";

            var document = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
            var memory = new PersistedMemory
            {
                Id = scopePrefix + IdGenerator.GetUniqueId(),
                Text = text,
                Retention = retentionPolicy.CreateInitialRetention(retentionTier)
            };

            document.Memories.Add(memory);

            await store.SaveAsync(document, cancellationToken).ConfigureAwait(false);
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
            var globalDocument = await globalStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            var projectDocument = await projectStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            if (GetUnknownMemoryId(memoryIds, globalDocument.Memories, projectDocument.Memories) is { } unknownMemoryId)
                return MemoryChangeResult.Reject($"Unknown memory '{unknownMemoryId}'.");

            // Decay should happen once per reinforcement cycle across all scopes.
            // Decay once per cycle, across all scopes.
            var changedGlobalRetention = false;
            var changedProjectRetention = false;

            if (tracker.Decayed())
            {
                changedGlobalRetention |= ApplyDecay(globalDocument);
                changedProjectRetention |= ApplyDecay(projectDocument);
            }

            // Reinforcement should apply even when decay already happened for this cycle.
            changedGlobalRetention |= ReinforceMemories(globalDocument, memoryIds);
            changedProjectRetention |= ReinforceMemories(projectDocument, memoryIds);

            if (changedGlobalRetention)
                await globalStore.SaveAsync(globalDocument, cancellationToken).ConfigureAwait(false);

            if (changedProjectRetention)
                await projectStore.SaveAsync(projectDocument, cancellationToken).ConfigureAwait(false);

            return MemoryChangeResult.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MemoryChangeResult> ForgetAsync(IReadOnlyList<string> memoryIds, CancellationToken cancellationToken = default)
    {
        if (memoryIds is null || memoryIds.Count == 0)
            return MemoryChangeResult.Reject("At least one memory id is required.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var requestedMemoryIds = memoryIds.ToHashSet(StringComparer.Ordinal);

            var globalDocument = await globalStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            var projectDocument = await projectStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            if (GetUnknownMemoryId(memoryIds, globalDocument.Memories, projectDocument.Memories) is { } unknownMemoryId)
                return MemoryChangeResult.Reject($"Unknown memory '{unknownMemoryId}'.");

            var removedGlobalCount = globalDocument.Memories.RemoveAll(memory => requestedMemoryIds.Contains(memory.Id));
            var removedProjectCount = projectDocument.Memories.RemoveAll(memory => requestedMemoryIds.Contains(memory.Id));

            if (removedGlobalCount > 0)
                await globalStore.SaveAsync(globalDocument, cancellationToken).ConfigureAwait(false);

            if (removedProjectCount > 0)
                await projectStore.SaveAsync(projectDocument, cancellationToken).ConfigureAwait(false);

            return MemoryChangeResult.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool PruneDeleteableMemories(PersistedMemoryDocument document) =>
        document.Memories.RemoveAll(memory => retentionPolicy.ShouldDelete(memory.Retention)) > 0;

    private IReadOnlyList<RecallMemory> RecallMemories(PersistedMemoryDocument globalDocument, PersistedMemoryDocument projectDocument) =>
        globalDocument.Memories
            .Concat(projectDocument.Memories)
            .OrderByDescending(memory => memory.Retention)
            .Select(memory => new RecallMemory(memory.Id, memory.Text))
            .ToArray();

    private static string? GetUnknownMemoryId(
        IReadOnlyList<string> memoryIds,
        IReadOnlyList<PersistedMemory> globalMemories,
        IReadOnlyList<PersistedMemory> projectMemories)
    {
        var knownMemoryIds = globalMemories
            .Concat(projectMemories)
            .Select(memory => memory.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var memoryId in memoryIds)
        {
            if (string.IsNullOrWhiteSpace(memoryId) || !knownMemoryIds.Contains(memoryId))
                return memoryId;
        }

        return null;
    }

    private bool ApplyGlobalWeakeningIfFirstTime(PersistedMemoryDocument document)
    {
        // Backwards-compatible helper used by older tests/flows.
        // (Kept for now; decay behavior is coordinated per-cycle in ReinforceAsync.)
        if (!tracker.Decayed())
            return false;

        return ApplyDecay(document);
    }

    private bool ApplyDecay(PersistedMemoryDocument document)
    {
        for (var index = 0; index < document.Memories.Count; index++)
        {
            var memory = document.Memories[index];
            document.Memories[index] = memory with { Retention = retentionPolicy.Decay(memory.Retention) };
        }

        return true;
    }

    private bool ReinforceMemories(PersistedMemoryDocument document, IReadOnlyList<string> memoryIds)
    {
        var requestedMemoryIds = memoryIds.ToHashSet(StringComparer.Ordinal);
        var changedRetention = false;

        for (var index = 0; index < document.Memories.Count; index++)
        {
            var memory = document.Memories[index];

            if (!requestedMemoryIds.Contains(memory.Id) || !tracker.Reinforced(memory.Id))
                continue;

            document.Memories[index] = memory with { Retention = retentionPolicy.Reinforce(memory.Retention) };
            changedRetention = true;
        }

        return changedRetention;
    }
}
