using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Features.Tests.TestDoubles;

internal sealed class FaultInjectingInMemoryStore(MemoryContainer container) : IMemoryStore
{
    public MemoryContainer Container { get; private set; } = CloneContainer(container);

    public bool FailAfterUpdate { get; set; }

    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpdateAsync(Action<MemoryContainer> update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        var clonedContainer = CloneContainer(Container);
        update(clonedContainer);

        if (FailAfterUpdate)
            throw new InvalidOperationException("Simulated save failure.");

        Container = clonedContainer;
        return Task.CompletedTask;
    }

    public Task<MemoryContainer> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CloneContainer(Container));
    }

    private static MemoryContainer CloneContainer(MemoryContainer container)
    {
        return new MemoryContainer
        {
            Memories = container.Memories.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(entry => new MemoryEntry(entry.Timestamp, entry.Text, entry.Tags, entry.Importance)).ToList(),
                StringComparer.Ordinal),
            CustomSections = [.. container.CustomSections.Select(summary => new MemorySectionSummary(summary.Name, summary.EntryCount))]
        };
    }
}
