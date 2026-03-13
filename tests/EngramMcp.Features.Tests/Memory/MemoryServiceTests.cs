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
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] = [],
            ["medium-term"] = [new(new DateTime(2026, 3, 11, 9, 0, 0), "existing")],
            ["long-term"] = []
        }));

        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

        await service.StoreAsync("short-term", "duplicate");
        await service.StoreAsync("short-term", "duplicate");

        memoryStore.Container.Memories["short-term"].Count.Is(2);
        memoryStore.Container.Memories["short-term"][0].Text.Is("duplicate");
        memoryStore.Container.Memories["short-term"][1].Text.Is("duplicate");
        memoryStore.Container.Memories["short-term"][0].Tags.Count.Is(0);
        memoryStore.Container.Memories["short-term"][0].Importance.Is(MemoryImportance.Normal);
        memoryStore.Container.Memories["medium-term"].Count.Is(1);
    }

    [Fact]
    public async Task StoreAsync_CreatesCustomBucketOnFirstWrite()
    {
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

        await service.StoreAsync("project-x", "custom");

        memoryStore.Container.Memories.ContainsKey("project-x").IsTrue();
        memoryStore.Container.Memories["project-x"].Select(entry => entry.Text).ToArray().SequenceEqual(["custom"]).IsTrue();
    }

    [Fact]
    public async Task StoreAsync_UsesSharedCustomBucketCapacity()
    {
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

        foreach (var index in Enumerable.Range(1, 55))
            await service.StoreAsync("project-x", $"entry-{index}");

        var entries = memoryStore.Container.Memories["project-x"];
        entries.Count.Is(50);
        entries[0].Text.Is("entry-6");
        entries[^1].Text.Is("entry-55");
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
    public async Task ReadAsync_ReturnsBuiltInSectionOnly()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] = [new(new DateTime(2026, 3, 11, 10, 0, 0), "short")],
            ["medium-term"] = [new(new DateTime(2026, 3, 11, 11, 0, 0), "medium")],
            ["long-term"] = []
        })));

        var result = await service.ReadAsync("short-term");

        result.Memories.Keys.ToArray().SequenceEqual(["short-term"]).IsTrue();
        result.Memories["short-term"].Select(entry => entry.Text).ToArray().SequenceEqual(["short"]).IsTrue();
    }

    [Fact]
    public async Task ReadAsync_ReturnsCustomSectionOnly()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] = [],
            ["medium-term"] = [],
            ["long-term"] = [],
            ["project-x"] = [new(new DateTime(2026, 3, 11, 12, 0, 0), "custom")]
        })));

        var result = await service.ReadAsync("project-x");

        result.Memories.Keys.ToArray().SequenceEqual(["project-x"]).IsTrue();
        result.Memories["project-x"].Select(entry => entry.Text).ToArray().SequenceEqual(["custom"]).IsTrue();
    }

    [Fact]
    public async Task ReadAsync_ThrowsWhenSectionDoesNotExist()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] = [],
            ["medium-term"] = [],
            ["long-term"] = [],
            ["project-a"] = [],
            ["project-z"] = [new(new DateTime(2026, 3, 11, 12, 0, 0), "custom")]
        })));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReadAsync("project-x"));

        exception.Message.Is("Memory section 'project-x' was not found. Available sections: long-term, medium-term, short-term, project-a, project-z.");
    }

    [Fact]
    public async Task RecallAsync_ReturnsConfiguredSectionsSeparatedByName()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] = [new(new DateTime(2026, 3, 11, 10, 0, 0), "short")],
            ["medium-term"] = [],
            ["long-term"] = [new(new DateTime(2026, 3, 11, 11, 0, 0), "long")]
        })));

        var recalled = await service.RecallAsync();

        recalled.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual(["long-term", "medium-term", "short-term"]).IsTrue();
        recalled.Memories["short-term"][0].Text.Is("short");
        recalled.Memories["long-term"][0].Text.Is("long");
    }

    [Fact]
    public async Task RecallAsync_ReturnsOnlyFixedBucketsInStableOrder()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["z-last"] = [new(new DateTime(2026, 3, 11, 12, 0, 0), "z")],
            ["short-term"] = [],
            ["medium-term"] = [],
            ["a-first"] = [new(new DateTime(2026, 3, 11, 13, 0, 0), "a")],
            ["long-term"] = []
        })));

        var recalled = await service.RecallAsync();

        recalled.Memories.Keys.ToArray().SequenceEqual(["long-term", "medium-term", "short-term"]).IsTrue();
        recalled.Memories.ContainsKey("a-first").IsFalse();
        recalled.Memories.ContainsKey("z-last").IsFalse();
    }

    [Fact]
    public async Task SearchAsync_MatchesSectionNameAcrossBuiltInAndCustomSections()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] =
            [
                new(new DateTime(2026, 3, 11, 10, 0, 0), "first short")
            ],
            ["medium-term"] = [],
            ["long-term"] = [],
            ["project-shortcuts"] =
            [
                new(new DateTime(2026, 3, 11, 11, 0, 0), "custom short")
            ]
        })));

        var results = await service.SearchAsync("short");

        results.Select(result => result.Section).ToArray().SequenceEqual(["project-shortcuts", "short-term"]).IsTrue();
    }

    [Fact]
    public async Task SearchAsync_MatchesTagsCaseInsensitively()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] =
            [
                new(new DateTime(2026, 3, 11, 10, 0, 0), "entry", ["Docker"])
            ],
            ["medium-term"] = [],
            ["long-term"] = []
        })));

        var results = await service.SearchAsync("DOCK");

        results.Count.Is(1);
        results[0].Entry.Text.Is("entry");
    }

    [Fact]
    public async Task SearchAsync_MatchesEntryTextCaseInsensitively()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] =
            [
                new(new DateTime(2026, 3, 11, 10, 0, 0), "Workspace drift is happening")
            ],
            ["medium-term"] = [],
            ["long-term"] = []
        })));

        var results = await service.SearchAsync("workspace DRIFT");

        results.Count.Is(1);
        results[0].Section.Is("short-term");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_RejectsEmptyOrWhitespaceQuery(string query)
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer()));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync(query));

        exception.Message.Is("Search query must not be null, empty, or whitespace. (Parameter 'query')");
    }

    [Fact]
    public async Task SearchAsync_SortsByImportanceDescendingThenTimestampDescending()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["short-term"] =
            [
                new(new DateTime(2026, 3, 11, 8, 0, 0), "match", importance: MemoryImportance.High),
                new(new DateTime(2026, 3, 11, 12, 0, 0), "match", importance: MemoryImportance.Normal),
                new(new DateTime(2026, 3, 11, 10, 0, 0), "match", importance: MemoryImportance.High)
            ],
            ["medium-term"] = [],
            ["long-term"] = []
        })));

        var results = await service.SearchAsync("match");

        results.Select(result => result.Entry.Timestamp).ToArray().SequenceEqual(
            [
                new DateTime(2026, 3, 11, 10, 0, 0),
                new DateTime(2026, 3, 11, 8, 0, 0),
                new DateTime(2026, 3, 11, 12, 0, 0)
            ]).IsTrue();
    }

    private static MemoryContainer CreateContainer(Dictionary<string, List<MemoryEntry>>? memories = null)
    {
        return new MemoryContainer
        {
            Memories = memories ?? new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["short-term"] = [],
                ["medium-term"] = [],
                ["long-term"] = []
            }
        };
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
                    pair => pair.Value.Select(entry => new MemoryEntry(entry.Timestamp, entry.Text, entry.Tags, entry.Importance)).ToList(),
                    StringComparer.Ordinal),
                CustomSections = [.. container.CustomSections.Select(summary => new MemorySectionSummary(summary.Name, summary.EntryCount))]
            };
        }
    }
}
