using EngramMcp.Tools.Memory.Identity;
using EngramMcp.Tools.Memory.Retention;
using EngramMcp.Tools.Memory.Session;
using EngramMcp.Tools.Memory.Storage;

namespace EngramMcp.Tools.Memory;

public sealed class MemoryService(
    Storage.IMemoryStore memoryStore,
    IMemoryIdGenerator memoryIdGenerator,
    IRetentionPolicy retentionPolicy,
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

            for (var index = 0; index < document.Memories.Count; index++)
            {
                var memory = document.Memories[index];
                document.Memories[index] = memory with { Retention = retentionPolicy.Decay(memory.Retention) };
            }

            document.Memories.RemoveAll(memory => retentionPolicy.ShouldDelete(memory.Retention));

            await memoryStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            return document.Memories
                .OrderByDescending(memory => memory.Retention)
                .Select(memory => new RecallMemory(memory.Id, memory.Text))
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RememberAsync(RetentionTier retentionTier, string text, CancellationToken cancellationToken = default)
    {
        text = MemoryText.Validate(text);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            var existingIds = document.Memories.Select(memory => memory.Id).ToArray();
            var memory = new PersistedMemory
            {
                Id = memoryIdGenerator.CreateId(existingIds, DateTime.Now),
                Text = text,
                Retention = retentionPolicy.CreateInitialRetention(retentionTier)
            };

            document.Memories.Add(memory);

            await memoryStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string?> ReinforceAsync(IReadOnlyList<string> memoryIds, CancellationToken cancellationToken = default)
    {
        if (memoryIds is null || memoryIds.Count == 0)
            return "At least one memory id is required.";

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var document = await LoadDocumentAsync(cancellationToken).ConfigureAwait(false);
            var memoriesById = document.Memories.ToDictionary(memory => memory.Id, StringComparer.Ordinal);

            foreach (var memoryId in memoryIds)
            {
                if (string.IsNullOrWhiteSpace(memoryId) || !memoriesById.ContainsKey(memoryId))
                    return $"Unknown memory '{memoryId}'.";
            }

            var updatedAnyRetention = false;

            foreach (var memoryId in memoryIds.Distinct(StringComparer.Ordinal))
            {
                if (!reinforcementTracker.MarkIfFirstTime(memoryId))
                    continue;

                var memory = memoriesById[memoryId];
                var updated = memory with { Retention = retentionPolicy.Reinforce(memory.Retention) };
                var index = document.Memories.FindIndex(existing => string.Equals(existing.Id, memoryId, StringComparison.Ordinal));
                document.Memories[index] = updated;
                updatedAnyRetention = true;
            }

            if (updatedAnyRetention)
                await memoryStore.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            return null;
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
}
