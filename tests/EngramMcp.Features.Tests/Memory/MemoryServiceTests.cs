using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using EngramMcp.Infrastructure.Memory;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Memory;

public sealed class MemoryServiceTests
{
    [Fact]
    public async Task StoreAsync_RejectsWhitespaceOnlyText()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["short-term"] = [],
                ["medium-term"] = [],
                ["long-term"] = []
            }
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => service.StoreAsync("short-term", "   "));
    }

    [Fact]
    public async Task StoreAsync_AllowsDuplicates_AndUsesTargetMemoryOnly()
    {
        var memoryStore = new InMemoryStore(new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["short-term"] = [],
                ["medium-term"] = [new(new DateTime(2026, 3, 11, 9, 0, 0), "existing")],
                ["long-term"] = []
            }
        });

        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

        await service.StoreAsync("short-term", "duplicate");
        await service.StoreAsync("short-term", "duplicate");

        memoryStore.Container.Memories["short-term"].Count.Is(2);
        memoryStore.Container.Memories["short-term"][0].Text.Is("duplicate");
        memoryStore.Container.Memories["short-term"][1].Text.Is("duplicate");
        memoryStore.Container.Memories["medium-term"].Count.Is(1);
    }

    [Fact]
    public async Task StoreAsync_SerializesConcurrentUpdatesAgainstJsonFileStore()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var filePath = Path.Combine(rootPath, "memory.json");
            var catalog = new CodeMemoryCatalog();
            var memoryStore = new JsonMemoryStore(filePath, catalog);
            var service = new MemoryService(catalog, memoryStore);
            var operations = Enumerable.Range(1, 10)
                .SelectMany(index => new[]
                {
                    service.StoreAsync("short-term", $"short-{index}"),
                    service.StoreAsync("medium-term", $"medium-{index}"),
                    service.StoreAsync("long-term", $"long-{index}")
                })
                .ToArray();

            await Task.WhenAll(operations);

            var recalled = await service.RecallAsync();

            recalled.Memories["short-term"].Count.Is(10);
            recalled.Memories["medium-term"].Count.Is(10);
            recalled.Memories["long-term"].Count.Is(10);
            recalled.Memories["short-term"].Select(entry => entry.Text).OrderBy(text => text).ToArray()
                .SequenceEqual(Enumerable.Range(1, 10).Select(index => $"short-{index}").OrderBy(text => text)).IsTrue();
            recalled.Memories["medium-term"].Select(entry => entry.Text).OrderBy(text => text).ToArray()
                .SequenceEqual(Enumerable.Range(1, 10).Select(index => $"medium-{index}").OrderBy(text => text)).IsTrue();
            recalled.Memories["long-term"].Select(entry => entry.Text).OrderBy(text => text).ToArray()
                .SequenceEqual(Enumerable.Range(1, 10).Select(index => $"long-{index}").OrderBy(text => text)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RecallAsync_ReturnsConfiguredSectionsSeparatedByName()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["short-term"] = [new(new DateTime(2026, 3, 11, 10, 0, 0), "short")],
                ["medium-term"] = [],
                ["long-term"] = [new(new DateTime(2026, 3, 11, 11, 0, 0), "long")]
            }
        }));

        var recalled = await service.RecallAsync();

        recalled.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual(["long-term", "medium-term", "short-term"]).IsTrue();
        recalled.Memories["short-term"][0].Text.Is("short");
        recalled.Memories["long-term"][0].Text.Is("long");
    }

    private sealed class InMemoryStore(MemoryContainer container) : IMemoryStore
    {
        public MemoryContainer Container { get; private set; } = container;

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Action<MemoryContainer> update, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(update);

            var container = Clone(Container);
            update(container);
            Container = container;
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
                    pair => pair.Value.Select(entry => new MemoryEntry(entry.Timestamp, entry.Text)).ToList(),
                    StringComparer.Ordinal)
            };
        }
    }
}
