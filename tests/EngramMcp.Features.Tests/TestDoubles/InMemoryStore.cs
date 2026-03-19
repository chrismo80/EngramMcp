using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Features.Tests.TestDoubles;

internal sealed class InMemoryStore(MemoryContainer container) : IMemoryStore
{
    public MemoryContainer Container { get; private set; } = container;

    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpdateAsync(Action<MemoryContainer> update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        var clonedContainer = Clone(container: Container);
        update(clonedContainer);
        Container = clonedContainer;
        return Task.CompletedTask;
    }

    public Task<MemoryContainer> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Clone(Container));
    }

    public Task SaveAsync(MemoryContainer container, CancellationToken cancellationToken = default)
    {
        Container = Clone(container);
        return Task.CompletedTask;
    }

    private static MemoryContainer Clone(MemoryContainer container)
    {
        return new MemoryContainer
        {
            Memories = container.Memories.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(entry => new MemoryEntry(entry.Timestamp, entry.Text, entry.Importance)).ToList(),
                StringComparer.Ordinal),
            CustomSections = [.. container.CustomSections.Select(summary => new MemorySectionSummary(summary.Name, summary.EntryCount))]
        };
    }
}
